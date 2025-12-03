using Bogus;
using MicroShop.StockAPI.Entities;

namespace MicroShop.StockAPI.Data;

public static class StockDbSeed
{
    public static void Seed(StockDbContext context)
    {
        // Kategori yoksa önce onları ekle
        if (!context.Categories.Any())
        {
            var categories = new List<Category>
            {
                new() { Name = "Elektronik" },   // Id: 1
                new() { Name = "Giyim" },        // Id: 2
                new() { Name = "Ev & Yaşam" },   // Id: 3
                new() { Name = "Spor & Outdoor" },// Id: 4
                new() { Name = "Kitap & Hobi" }  // Id: 5
            };
            context.Categories.AddRange(categories);
            context.SaveChanges(); // ID'lerin oluşması için kaydet
        }

        // Ürün yoksa ekle
        if (!context.Products.Any())
        {
            Console.WriteLine("--> Seed Data: Ürünler oluşturuluyor...");

            // Kategori ID'lerini manuel veriyoruz (Yukarıdaki sıraya göre 1=Elektronik, 2=Giyim vs.)
            var staticProducts = new List<Product>
            {
                new Product
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Name = "MacBook Pro M3",
                    Description = "14 inç, 16GB Ram",
                    CategoryId = 1, // Elektronik
                    Price = 75000.00m,
                    StockCount = 10,
                    PictureUrl = "https://store.storeimages.cdn-apple.com/4668/as-images.apple.com/is/mbp14-spacegray-select-202310?wid=904&hei=840&fmt=jpeg&qlt=90&.v=1697311054290",
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Name = "iPhone 15",
                    CategoryId = 1, // Elektronik
                    Price = 50000.00m,
                    StockCount = 50,
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Name = "Nike Air Max",
                    CategoryId = 2, // Giyim
                    Price = 4500.00m,
                    StockCount = 100,
                    CreatedAt = DateTime.UtcNow
                }
            };
            context.Products.AddRange(staticProducts);

            // 2. RASTGELE ÜRÜNLER (Bogus)
            var categoryIds = context.Categories.Select(c => c.Id).ToList(); // Mevcut kategori ID'leri

            var faker = new Faker<Product>("tr")
                .RuleFor(p => p.Id, f => Guid.NewGuid())
                .RuleFor(p => p.Name, f => f.Commerce.ProductName())
                .RuleFor(p => p.Description, f => f.Commerce.ProductDescription())
                .RuleFor(p => p.CategoryId, f => f.PickRandom(categoryIds))
                .RuleFor(p => p.Price, f => decimal.Parse(f.Commerce.Price(100, 10000)))
                .RuleFor(p => p.StockCount, f => f.Random.Number(1, 100))
                .RuleFor(p => p.PictureUrl, f => f.Image.PicsumUrl())
                .RuleFor(p => p.CreatedAt, f => DateTime.UtcNow);

            var randomProducts = faker.Generate(300);
            context.Products.AddRange(randomProducts);

            context.SaveChanges();
        }
    }
}