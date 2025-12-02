namespace MicroShop.OrderAPI.Dtos;

public class CreateOrderDto
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}