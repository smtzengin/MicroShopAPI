using MicroShop.PaymentAPI.Entities;
using MicroShop.Shared.Interfaces;

namespace MicroShop.PaymentAPI.Services;

public class PaymentService
{
    private readonly IUnitOfWork _uow;

    public PaymentService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> ProcessPaymentAsync(Guid orderId, Guid userId, decimal amount, string? couponCode)
    {
        var wallet = await _uow.Repository<Wallet>().GetAllAsync(); // Doğrusu GetByUserId ama generic repo ile böyle
        var userWallet = wallet.FirstOrDefault(w => w.UserId == userId);

        if (userWallet == null)
        {
            await LogTransaction(orderId, amount, "Fail", "Müşteri Bulunamadı");
            return false;
        }

        // 2. KUPON KONTROLÜ (YENİ MANTIK)
        decimal finalAmount = amount;
        string appliedCoupon = "Yok";

        if (!string.IsNullOrEmpty(couponCode))
        {
            var coupons = await _uow.Repository<Coupon>().GetAllAsync();
            var coupon = coupons.FirstOrDefault(c => c.Code == couponCode && c.IsActive);

            if (coupon != null)
            {
                // İndirimi uygula (Tutar eksiye düşmesin diye Math.Max)
                finalAmount = Math.Max(0, amount - coupon.DiscountAmount);
                appliedCoupon = $"{coupon.Code} ({coupon.DiscountAmount} TL İndirim)";
                Console.WriteLine($"[Payment] Kupon Uygulandı! Eski: {amount}, Yeni: {finalAmount}");
            }
            else
            {
                // Kupon geçersizse hata verme, sadece uygulama.
                Console.WriteLine($"[Payment] Geçersiz Kupon: {couponCode}");
            }
        }

        // 3. Bakiye Kontrolü (İndirimli fiyat üzerinden)
        if (userWallet.Balance < finalAmount)
        {
            await LogTransaction(orderId, finalAmount, "Fail", $"Yetersiz Bakiye! (İstenen: {finalAmount})");
            return false;
        }

        // 4. Parayı Çek
        userWallet.Balance -= finalAmount;
        await _uow.SaveChangesAsync();

        await LogTransaction(orderId, finalAmount, "Success", $"Kupon: {appliedCoupon}");
        return true;
    }
    private async Task LogTransaction(Guid orderId, decimal amount, string status, string? error)
    {
        var log = new PaymentLog
        {
            OrderId = orderId,
            Amount = amount,
            TransactionId = Guid.NewGuid().ToString(),
            Status = status,
            ErrorMessage = error
        };
        await _uow.Repository<PaymentLog>().AddAsync(log);
        await _uow.SaveChangesAsync();
    }
}