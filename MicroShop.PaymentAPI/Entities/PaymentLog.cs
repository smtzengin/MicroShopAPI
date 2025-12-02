using MicroShop.Shared.Models;

namespace MicroShop.PaymentAPI.Entities;

public class PaymentLog : BaseEntity
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string TransactionId { get; set; } // Bankadan dönen referans no (uyduracağız)
    public string Status { get; set; } // "Success" veya "Fail"
    public string? ErrorMessage { get; set; }
}