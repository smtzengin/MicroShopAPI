
namespace MicroShop.Shared.Models;

public enum OrderState
{
    Created,        // Sipariş ilk oluştuğunda
    StockReserved,  // Stok başarıyla ayrıldığında
    PaymentTaken,   // Ödeme başarıyla alındığında
    Failed,         // Herhangi bir hata durumunda
    Completed       // Süreç tamamen bittiğinde
}