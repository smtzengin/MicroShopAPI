using MicroShop.OrderAPI.Models;
using MicroShop.Shared.Models;

namespace MicroShop.OrderAPI.Entities;

public class Order : BaseEntity
{
    public Guid UserId { get; set; } 

    public Guid BuyerId { get; set; }

    public decimal TotalPrice { get; set; }
    public OrderState Status { get; set; }
    public string? FailReason { get; set; }

    // --- YENİ ALANLAR ---
    public Address ShippingAddress { get; set; } // Adres
    public List<OrderItem> Items { get; set; } = new List<OrderItem>(); // Ürünler

    public string? CouponCode { get; set; }
}