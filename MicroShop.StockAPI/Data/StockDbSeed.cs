using Bogus;
using MicroShop.StockAPI.Entities;

namespace MicroShop.StockAPI.Data;

public static class StockDbSeed
{
    public static void Seed(StockDbContext context)
    {
        // Eğer veritabanında hiç ürün yoksa ekle
        if (!context.Products.Any())
        {
            Console.WriteLine("--> Seed Data: Ürünler oluşturuluyor...");

            // 1. SABİT ÜRÜNLER (Test Senaryoların İçin Bunlar Şart)
            // Bu ID'leri Swagger'da ve Postman'de kullanıyorsun, o yüzden elle ekliyoruz.
            var staticProducts = new List<Product>
            {
                new Product
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Name = "MacBook Pro M3 (Test)",
                    Description = "14 inç, 16GB Ram, Test Ürünü",
                    Category = "Elektronik",
                    Price = 75000.00m,
                    StockCount = 10,
                    PictureUrl = "https://store.storeimages.cdn-apple.com/4668/as-images.apple.com/is/mbp14-spacegray-select-202310?wid=904&hei=840&fmt=jpeg&qlt=90&.v=1697311054290",
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Name = "iPhone 15 (Test)",
                    Description = "Test Ürünü",
                    Category = "Telefon",
                    Price = 50000.00m,
                    StockCount = 50,
                    PictureUrl = "https://store.storeimages.cdn-apple.com/4668/as-images.apple.com/is/iphone-15-black-select-202309?wid=5120&hei=2880&fmt=p-jpg&qlt=80&.v=1692924322785",
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Name = "Logitech MX Master 3S",
                    Description = "Kablosuz Mouse",
                    Category = "Aksesuar",
                    Price = 4500.00m,
                    StockCount = 100,
                    PictureUrl = "https://resource.logitech.com/w_692,c_lpad,ar_4:3,q_auto,f_auto,dpr_1.0/d_transparent.gif/content/dam/logitech/en/products/mice/mx-master-3s/gallery/mx-master-3s-mouse-top-view-graphite.png?v=1",
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.Products.AddRange(staticProducts);

            // 2. RASTGELE ÜRÜNLER (Bogus ile 300 Tane)
            // Kuralları belirliyoruz:
            var faker = new Faker<Product>("tr") // Türkçe veri üret
                .RuleFor(p => p.Id, f => Guid.NewGuid())
                .RuleFor(p => p.Name, f => f.Commerce.ProductName())
                .RuleFor(p => p.Description, f => f.Commerce.ProductDescription())
                .RuleFor(p => p.Category, f => f.PickRandom("Elektronik", "Giyim", "Ev & Yaşam", "Spor", "Kitap"))
                .RuleFor(p => p.Price, f => decimal.Parse(f.Commerce.Price(100, 10000))) // 100 ile 10.000 TL arası
                .RuleFor(p => p.StockCount, f => f.Random.Number(1, 100))
                .RuleFor(p => p.PictureUrl, f => f.Image.PicsumUrl()) // Rastgele resim URL'i
                .RuleFor(p => p.CreatedAt, f => DateTime.UtcNow);

            var randomProducts = faker.Generate(300); // 300 tane üret

            context.Products.AddRange(randomProducts);

            // Veritabanına kaydet
            context.SaveChanges();

            Console.WriteLine("--> Seed Data: 303 Ürün başarıyla eklendi!");
        }
    }
}