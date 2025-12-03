namespace MicroShop.OrderAPI.Dtos;

public class OrderItemDto
{
    public Guid ProductId { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string ProductName { get; set; }
}