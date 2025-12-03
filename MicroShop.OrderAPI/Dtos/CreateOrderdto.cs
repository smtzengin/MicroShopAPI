using MicroShop.Shared.Models;

namespace MicroShop.OrderAPI.Dtos;

public class CreateOrderDto
{
    public Guid UserId { get; set; }
    public AddressDto Address { get; set; }
    public List<OrderItemDto> Items { get; set; }
    public string? CouponCode { get; set; }
    public PaymentType PaymentType { get; set; }
    public CreditCardDto? CardInfo { get; set; }
}