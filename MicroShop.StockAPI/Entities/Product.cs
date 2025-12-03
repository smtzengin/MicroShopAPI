using MicroShop.Shared.Models;

namespace MicroShop.StockAPI.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int StockCount { get; set; }
    public string? PictureUrl { get; set; }

    public int CategoryId { get; set; } // YENİ (Foreign Key)
    public Category Category { get; set; } // Navigation Property
}