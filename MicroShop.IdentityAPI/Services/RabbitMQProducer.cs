using MicroShop.Shared.Interfaces;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace MicroShop.IdentityAPI.Services;

public class RabbitMQProducer : IMessageProducer
{
    private readonly ConnectionFactory _factory;
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMQProducer()
    {
        _factory = new ConnectionFactory { HostName = "localhost" };
        _connection = _factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    public void SendMessage<T>(T message, string queueName)
    {
        // Kuyruğu garantiye al
        _channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        _channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: null, body: body);
    }
}