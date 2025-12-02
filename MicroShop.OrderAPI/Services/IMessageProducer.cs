namespace MicroShop.OrderAPI.Services;

public interface IMessageProducer
{
    void SendMessage<T>(T message);
}