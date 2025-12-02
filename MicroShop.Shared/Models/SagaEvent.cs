
namespace MicroShop.Shared.Models;

public class SagaEvent
{
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }

    public Guid UserId { get; set; }
    public decimal TotalPrice { get; set; }
    public int Quantity { get; set; }

    // Sürecin şu anki durumu
    public OrderState CurrentState { get; set; }

    // İşlem başarılı mı? (True: Devam et, False: Rollback yap)
    public bool IsSuccess { get; set; } = true;

    // Hata varsa sebebi (Örn: "Yetersiz Bakiye")
    public string? ErrorMessage { get; set; }

    // Bu bir geri alma (iade) işlemi mi?
    public bool IsCompensating { get; set; }
}