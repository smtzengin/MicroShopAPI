using MicroShop.Shared.Models;

namespace MicroShop.PaymentAPI.Entities;

public class Coupon : BaseEntity
{
    public string Code { get; set; } 
    public decimal DiscountAmount { get; set; } 
    public bool IsActive { get; set; } = true;
}
