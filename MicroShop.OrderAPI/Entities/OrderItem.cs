
using MicroShop.OrderAPI.Entities;
using MicroShop.Shared.Models;

namespace MicroShop.OrderAPI.Models;

public class OrderItem : BaseEntity
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }

    // Hangi siparişe ait?
    public Guid OrderId { get; set; }
    public Order Order { get; set; }
}