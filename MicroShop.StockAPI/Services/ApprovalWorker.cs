using System.Text;
using System.Text.Json;
using MicroShop.Shared.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MicroShop.StockAPI.Services;

public class ApprovalWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private IConnection _connection;
    private IModel _channel;

    public ApprovalWorker(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        var factory = new ConnectionFactory { HostName = "localhost" };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(queue: "queue.product.approval", durable: false, exclusive: false, autoDelete: false, arguments: null);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var productEvent = JsonSerializer.Deserialize<ProductCreatedEvent>(json, options);

            Console.WriteLine($"[ApprovalWorker] İnceleme Başladı: {productEvent.ProductName}");

            // YAPAY GECİKME (Simülasyon: "Yasaklı kelime taraması yapılıyor...")
            await Task.Delay(10000); // 10 Saniye bekle

            using var scope = _serviceProvider.CreateScope();
            var stockService = scope.ServiceProvider.GetRequiredService<StockService>();

            // Yasaklı kelime kontrolü (Şaka amaçlı :) )
            if (productEvent.Description.Contains("bomba") || productEvent.Description.Contains("silah"))
            {
                Console.WriteLine($"[ApprovalWorker] REDDEDİLDİ: {productEvent.ProductName}");
                // Status Rejected yapılabilir
            }
            else
            {
                await stockService.ApproveProductAsync(productEvent.ProductId);
                Console.WriteLine($"[ApprovalWorker] ONAYLANDI ve YAYINDA: {productEvent.ProductName}");
            }
        };

        _channel.BasicConsume(queue: "queue.product.approval", autoAck: true, consumer: consumer);
        await Task.CompletedTask;
    }
}