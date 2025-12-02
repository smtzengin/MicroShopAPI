using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace MicroShop.OrderAPI.Services;

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

        _channel.QueueDeclare(queue: "queue.stock", durable: false, exclusive: false, autoDelete: false, arguments: null);
    }

    public void SendMessage<T>(T message)
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        _channel.BasicPublish(exchange: "", routingKey: "queue.stock", basicProperties: null, body: body);
    }
}
