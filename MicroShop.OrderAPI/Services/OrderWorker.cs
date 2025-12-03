using MicroShop.OrderAPI.Entities;
using MicroShop.Shared.Interfaces;
using MicroShop.Shared.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace MicroShop.OrderAPI.Services;

public class OrderWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private IConnection _connection;
    private IModel _channel;

    public OrderWorker(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        var factory = new ConnectionFactory { HostName = "localhost" };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

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
            var sagaEvent = JsonSerializer.Deserialize<SagaEvent>(json, options););

            using var scope = _serviceProvider.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var messageProducer = scope.ServiceProvider.GetRequiredService<IMessageProducer>(); 

            Console.WriteLine($"[Orchestrator] Mesaj Geldi. State: {sagaEvent.CurrentState} | Success: {sagaEvent.IsSuccess}");

            var order = await uow.Repository<Order>().GetByIdAsync(sagaEvent.OrderId);
            if (order == null) return;

            // --- SENARYO 1: HATA VE ROLLBACK ---
            if (!sagaEvent.IsSuccess)
            {
                order.Status = OrderState.Failed;
                order.FailReason = sagaEvent.ErrorMessage;
                await uow.SaveChangesAsync();

                Console.WriteLine($"[Orchestrator] HATA ALINDI! Rollback Başlatılıyor... Sebep: {sagaEvent.ErrorMessage}");

                // Hata Payment'tan geldiyse Stoğu geri ver
                // (StockReserved durumundayken patladıysa, stok düşülmüş demektir)
                if (sagaEvent.CurrentState == OrderState.PaymentTaken || sagaEvent.CurrentState == OrderState.StockReserved)
                {
                    sagaEvent.IsCompensating = true;
                    // Stoğu geri alması için Stock kuyruğuna atıyoruz
                    // (Sadece kuyruk adını değiştirip publish ediyoruz)
                    // Şimdilik manuel publish edelim veya Producer'ı güncelleyelim.
       
                    var rollbackJson = JsonSerializer.Serialize(sagaEvent);
                    var rollbackBody = Encoding.UTF8.GetBytes(rollbackJson);
                    _channel.BasicPublish("", "queue.stock", null, rollbackBody);
                }
                return;
            }

            // --- SENARYO 2: BAŞARILI AKIŞ ---
            switch (sagaEvent.CurrentState)
            {
                case OrderState.StockReserved:
                    // Stok OK, Sırada Ödeme Var
                    order.Status = OrderState.StockReserved;
                    await uow.SaveChangesAsync();

                    Console.WriteLine("[Orchestrator] Stok Tamam -> Ödeme'ye gönderiliyor.");

                    // Payment Kuyruğuna Gönder
                    var payJson = JsonSerializer.Serialize(sagaEvent);
                    _channel.BasicPublish("", "queue.payment", null, Encoding.UTF8.GetBytes(payJson));
                    break;

                case OrderState.PaymentTaken:
                    // Ödeme OK, Her şey bitti
                    order.Status = OrderState.Completed;
                    await uow.SaveChangesAsync();
                    Console.WriteLine("[Orchestrator] SİPARİŞ BAŞARIYLA TAMAMLANDI! 🎉");
                    break;
            }
        };

        _channel.BasicConsume(queue: "queue.orchestrator", autoAck: true, consumer: consumer);
        await Task.CompletedTask;
    }
}