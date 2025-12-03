using System.Text;
using System.Text.Json;
using MicroShop.PaymentAPI.Entities;
using MicroShop.Shared.Interfaces;
using MicroShop.Shared.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MicroShop.PaymentAPI.Services;

public class IdentityWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private IConnection _connection;
    private IModel _channel;

    public IdentityWorker(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        var factory = new ConnectionFactory { HostName = "localhost" };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Dinlenecek Kuyruk
        _channel.QueueDeclare(queue: "queue.identity.user-created", durable: false, exclusive: false, autoDelete: false, arguments: null);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);

            // Case-Insensitive ayarı (BOŞ GUID HATASI OLMASIN DİYE)
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var userEvent = JsonSerializer.Deserialize<UserCreatedEvent>(json, options);

            using var scope = _serviceProvider.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            try
            {
                // Cüzdan var mı kontrol et (Idempotency)
                var existingWallet = await uow.Repository<Wallet>().GetByIdAsync(userEvent.UserId);

                if (existingWallet == null)
                {
                    var newWallet = new Wallet
                    {
                        UserId = userEvent.UserId, // Gelen ID
                        OwnerName = userEvent.FullName,
                        Balance = 0, // Yeni kullanıcıya 0 bakiye (veya hoşgeldin bonusu 100 verelim mi? :) )
                        CreatedAt = DateTime.UtcNow
                    };

                    await uow.Repository<Wallet>().AddAsync(newWallet);
                    await uow.SaveChangesAsync();

                    Console.WriteLine($"[IdentityWorker] Yeni Cüzdan Oluşturuldu: {userEvent.FullName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IdentityWorker] Hata: {ex.Message}");
            }
        };

        _channel.BasicConsume(queue: "queue.identity.user-created", autoAck: true, consumer: consumer);
        await Task.CompletedTask;
    }
}