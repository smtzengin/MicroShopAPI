namespace MicroShop.StockAPI.Models;

public class ProductFilterParams
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public string? Search { get; set; }

    // Değişiklik: string Category yerine int? CategoryId
    public int? CategoryId { get; set; }

    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    public Guid? SellerId { get; set; } 
    public bool OnlyApproved { get; set; } = true; 
}