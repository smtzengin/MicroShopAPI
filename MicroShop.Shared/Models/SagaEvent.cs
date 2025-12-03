
namespace MicroShop.Shared.Models;

public class SagaEvent
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public decimal TotalPrice { get; set; }
    public List<SagaOrderItem> Items { get; set; } = new List<SagaOrderItem>();

    public OrderState CurrentState { get; set; }
    public bool IsSuccess { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public bool IsCompensating { get; set; }

    public string? CouponCode { get; set; }
    public PaymentType PaymentType { get; set; } // Seçilen yöntem
    public CreditCardInfo? CardInfo { get; set; } // Kart bilgileri (Opsiyonel)
}

public class SagaOrderItem
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}