using MicroShop.Shared.Models;

namespace MicroShop.StockAPI.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public decimal Price { get; set; }
    public int StockCount { get; set; }
    public string? PictureUrl { get; set; }
}