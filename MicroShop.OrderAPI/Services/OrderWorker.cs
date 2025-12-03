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
            var sagaEvent = JsonSerializer.Deserialize<SagaEvent>(json, options);

            using var scope = _serviceProvider.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var messageProducer = scope.ServiceProvider.GetRequiredService<IMessageProducer>(); 

            Console.WriteLine($"[Orchestrator] Mesaj Geldi. State: {sagaEvent.CurrentState} | Success: {sagaEvent.IsSuccess}");

            var order = await uow.Repository<Order>().GetByIdAsync(sagaEvent.OrderId);
            if (order == null) return;

            // --- SENARYO 1: HATA VE ROLLBACK ---
            if (!sagaEvent.IsSuccess)
            {
                //  Önce veritabanına "Hata Aldım" diye yaz
                order.Status = OrderState.Failed;
                order.FailReason = $"Hata Aşaması: {sagaEvent.CurrentState} - Mesaj: {sagaEvent.ErrorMessage}";
                await uow.SaveChangesAsync();

                Console.WriteLine($"[Orchestrator] HATA TESPİT EDİLDİ: {order.FailReason}");

                // Hangi aşamada patladık? Geriye doğru telafi (Compensate) zinciri
                switch (sagaEvent.CurrentState)
                {
                    case OrderState.PaymentTaken:
                    case OrderState.StockReserved:
                        // "Ödeme Alınırken" veya "Stok Ayrıldıktan Sonra" hata geldiyse
                        // Kesinlikle Stok düşülmüştür. STOK İADESİ BAŞLAT.

                        Console.WriteLine("[Orchestrator] Kritik Hata: Stok İadesi (Compensate) Başlatılıyor...");

                        sagaEvent.IsCompensating = true;
                        messageProducer.SendMessage(sagaEvent, "queue.stock");
                        break;

                    case OrderState.Created:
                        Console.WriteLine("[Orchestrator] İlk adımda (Stok) hata alındı. Rollback gerekmez. İşlem bitti.");
                        break;
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