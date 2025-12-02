using MicroShop.PaymentAPI.Data;
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

    public async Task<bool> ProcessPaymentAsync(Guid orderId, Guid userId, decimal amount)
    {
        var wallets = await _uow.Repository<Wallet>().GetAllAsync();
        var userWallet = wallets.FirstOrDefault(w => w.UserId == userId);

        // Müşteri yoksa hata
        if (userWallet == null)
        {
            await LogTransaction(orderId, amount, "Fail", "Müşteri Bulunamadı");
            return false;
        }

        // 2. Bakiye Kontrolü
        if (userWallet.Balance < amount)
        {
            await LogTransaction(orderId, amount, "Fail", $"Yetersiz Bakiye! (Mevcut: {userWallet.Balance} TL)");
            return false;
        }

        // 3. Parayı Çek
        userWallet.Balance -= amount;
        await _uow.SaveChangesAsync(); 

        await LogTransaction(orderId, amount, "Success", null);
        Console.WriteLine($"[Payment] Ödeme Alındı. Kalan Bakiye: {userWallet.Balance}");

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