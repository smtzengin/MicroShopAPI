using MicroShop.PaymentAPI.Entities;
using MicroShop.Shared.Interfaces;
using MicroShop.Shared.Models;

namespace MicroShop.PaymentAPI.Services;

public class PaymentService
{
    private readonly IUnitOfWork _uow;

    public PaymentService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> ProcessPaymentAsync(Guid orderId, Guid userId, decimal amount, string? couponCode, PaymentType paymentType, CreditCardInfo? cardInfo)
    {
      
        decimal finalAmount = amount;
        string appliedCoupon = "Yok";

        if (!string.IsNullOrEmpty(couponCode))
        {

            var coupons = await _uow.Repository<Coupon>().GetAllAsync();
            var coupon = coupons.FirstOrDefault(c => c.Code == couponCode && c.IsActive);

            if (coupon != null)
            {
                // İndirimi uygula (Tutar eksiye düşmesin diye kontrol)
                finalAmount = Math.Max(0, amount - coupon.DiscountAmount);
                appliedCoupon = $"{coupon.Code} ({coupon.DiscountAmount} TL İndirim)";

                Console.WriteLine($"[Payment] Kupon Uygulandı! Kod: {coupon.Code}, Eski: {amount}, Yeni: {finalAmount}");
            }
            else
            {
                Console.WriteLine($"[Payment] Geçersiz Kupon Kodu: {couponCode}");
            }
        }


        // --- SENARYO A: CÜZDAN İLE ÖDEME ---
        if (paymentType == PaymentType.Wallet)
        {
            var wallets = await _uow.Repository<Wallet>().GetAllAsync();
            var userWallet = wallets.FirstOrDefault(w => w.UserId == userId);

            // Cüzdan kontrolü
            if (userWallet == null)
            {
                await LogTransaction(orderId, amount, "Fail", "Müşteri Cüzdanı Bulunamadı");
                return false;
            }

            // Bakiye Kontrolü (İndirimli fiyat üzerinden)
            if (userWallet.Balance < finalAmount)
            {
                await LogTransaction(orderId, finalAmount, "Fail", $"Yetersiz Bakiye (Cüzdan)! (Mevcut: {userWallet.Balance})");
                return false;
            }

            // Parayı Çek
            userWallet.Balance -= finalAmount;
            await _uow.SaveChangesAsync();

            await LogTransaction(orderId, finalAmount, "Success", $"Cüzdan - Kupon: {appliedCoupon}");
            Console.WriteLine($"[Payment] Cüzdan Ödemesi Başarılı. Kalan: {userWallet.Balance}");
            return true;
        }

        else if (paymentType == PaymentType.CreditCard)
        {
            // Kart bilgisi gelmiş mi?
            if (cardInfo == null)
            {
                await LogTransaction(orderId, finalAmount, "Fail", "Kart Bilgileri Eksik");
                return false;
            }

            // Simülasyon Kuralı: Kart numarası "00" ile bitiyorsa REDDET (Limit Yetersiz vb.)
            if (cardInfo.CardNumber.EndsWith("00"))
            {
                await LogTransaction(orderId, finalAmount, "Fail", "Kart Reddedildi (Limit/Banka Hatası)");
                Console.WriteLine("[Payment] Sanal POS: Kart Reddedildi.");
                return false;
            }


            await LogTransaction(orderId, finalAmount, "Success", $"Kredi Kartı - Kupon: {appliedCoupon}");
            Console.WriteLine($"[Payment] Sanal POS: {finalAmount} TL başarıyla çekildi.");
            return true;
        }

        return false; // Bilinmeyen ödeme tipi
    }

    // İşlem Loglama Yardımcısı
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