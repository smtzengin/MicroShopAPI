using MicroShop.Shared.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace MicroShop.StockAPI.Services;

public class StockWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private IConnection _connection;
    private IModel _channel;

    public StockWorker(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        var factory = new ConnectionFactory { HostName = "localhost" };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(queue: "queue.stock", durable: false, exclusive: false, autoDelete: false, arguments: null);

        _channel.QueueDeclare(queue: "queue.orchestrator", durable: false, exclusive: false, autoDelete: false, arguments: null);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var sagaEvent = JsonSerializer.Deserialize<SagaEvent>(json, options);
            
            Console.WriteLine($"[StockWorker] Mesaj Geldi. Items: {sagaEvent.Items}");

            using var scope = _serviceProvider.CreateScope();
            var stockService = scope.ServiceProvider.GetRequiredService<StockService>();

            try
            {
                if (sagaEvent.IsCompensating)
                {
                    // --- ROLLBACK (İADE) ---
                    await stockService.ReleaseStockAsync(sagaEvent.Items);
                    sagaEvent.CurrentState = OrderState.Created;
                }
                else
                {
                    bool result = await stockService.ReserveStockAsync(sagaEvent.Items);

                    if (result)
                    {
                        sagaEvent.CurrentState = OrderState.StockReserved;
                        sagaEvent.IsSuccess = true;
                    }
                    else
                    {
                        sagaEvent.IsSuccess = false;
                        sagaEvent.ErrorMessage = "Stok Yetersiz (Bazı ürünler eksik)";
                    }
                }
            }
            catch (Exception ex)
            {
                sagaEvent.IsSuccess = false;
                sagaEvent.ErrorMessage = ex.Message;
                Console.WriteLine($"[StockWorker] EXCEPTION: {ex.Message}");
            }

            // Sonucu Orchestrator'a bildir
            SendToOrchestrator(sagaEvent);
        };

        _channel.BasicConsume(queue: "queue.stock", autoAck: true, consumer: consumer);

        await Task.CompletedTask;
    }

    private void SendToOrchestrator(SagaEvent sagaEvent)
    {
        var json = JsonSerializer.Serialize(sagaEvent);
        var body = Encoding.UTF8.GetBytes(json);
        _channel.BasicPublish(exchange: "", routingKey: "queue.orchestrator", basicProperties: null, body: body);
    }
}
