using MicroShop.Shared.Models;

namespace MicroShop.PaymentAPI.Data;

public class Wallet : BaseEntity
{
    public Guid UserId { get; set; } // Müşteri No
    public decimal Balance { get; set; } // Bakiyesi
    public string OwnerName { get; set; } // Adı Soyadı (Test için)
}