namespace MicroShop.OrderAPI.Dtos;

public class CreateOrderDto
{
    public Guid UserId { get; set; }
    public AddressDto Address { get; set; }
    public List<OrderItemDto> Items { get; set; }

    public string? CouponCode { get; set; }
}