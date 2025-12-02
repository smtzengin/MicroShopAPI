namespace MicroShop.PaymentAPI.Data;

public static class PaymentDbSeed
{
    public static void Seed(PaymentDbContext context)
    {
        if (!context.Wallets.Any())
        {
            context.Wallets.AddRange(
                // Müşteri 1: Zengin (Hesabında 100.000 TL var)
                new Wallet { UserId = new Guid(), OwnerName = "Elon Musk", Balance = 100000m, CreatedAt = DateTime.UtcNow },

                // Müşteri 2: Bakiyesiz (Hesabında 0 TL var)
                new Wallet { UserId = new Guid(), OwnerName = "Gariban Öğrenci", Balance = 0m, CreatedAt = DateTime.UtcNow }
            );
            context.SaveChanges();
        }
    }
}