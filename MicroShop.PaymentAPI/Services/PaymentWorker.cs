using MicroShop.Shared.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace MicroShop.PaymentAPI.Services;

public class PaymentWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private IConnection _connection;
    private IModel _channel;

    public PaymentWorker(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        var factory = new ConnectionFactory { HostName = "localhost" };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // 1. Dinlenecek Kuyruk
        _channel.QueueDeclare(queue: "queue.payment", durable: false, exclusive: false, autoDelete: false, arguments: null);

        // 2. Cevap Kuyruğu
        _channel.QueueDeclare(queue: "queue.orchestrator", durable: false, exclusive: false, autoDelete: false, arguments: null);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var sagaEvent = JsonSerializer.Deserialize<SagaEvent>(json, options);

            Console.WriteLine($"[PaymentWorker] Ödeme Emri Geldi. OrderId: {sagaEvent.OrderId} | Tutar: {sagaEvent.TotalPrice}");
            using var scope = _serviceProvider.CreateScope();
            var paymentService = scope.ServiceProvider.GetRequiredService<PaymentService>();

            try
            {
                // Ödeme servisi normal akışta çalışır. 
                // Ödemenin "Rollback"i genelde para iadesidir ama burada işlem başarısız olursa zaten para çekilmez.
                if (!sagaEvent.IsCompensating)
                {
                    bool success = await paymentService.ProcessPaymentAsync(sagaEvent.OrderId,sagaEvent.UserId,  sagaEvent.TotalPrice);

                    if (success)
                    {
                        sagaEvent.IsSuccess = true;
                        sagaEvent.CurrentState = OrderState.PaymentTaken; // Durumu ilerlet
                    }
                    else
                    {
                        sagaEvent.IsSuccess = false;
                        sagaEvent.ErrorMessage = "Yetersiz Bakiye";
                        // Durumu değiştirmiyoruz, fail olduğunu Orchestrator anlayacak
                    }

                    SendToOrchestrator(sagaEvent);
                }
            }
            catch (Exception ex)
            {
                sagaEvent.IsSuccess = false;
                sagaEvent.ErrorMessage = ex.Message;
                SendToOrchestrator(sagaEvent);
            }
        };

        _channel.BasicConsume(queue: "queue.payment", autoAck: true, consumer: consumer);
        await Task.CompletedTask;
    }

    private void SendToOrchestrator(SagaEvent sagaEvent)
    {
        var json = JsonSerializer.Serialize(sagaEvent);
        var body = Encoding.UTF8.GetBytes(json);
        _channel.BasicPublish(exchange: "", routingKey: "queue.orchestrator", basicProperties: null, body: body);
    }
}