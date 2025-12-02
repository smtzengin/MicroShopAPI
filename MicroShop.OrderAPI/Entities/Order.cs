using MicroShop.Shared.Models;

namespace MicroShop.OrderAPI.Entities;

public class Order : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }

    // Status Enum'ı
    public OrderState Status { get; set; }

    public string? FailReason { get; set; }
}