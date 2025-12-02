using MicroShop.Shared.Interfaces;
using MicroShop.StockAPI.Entities;

namespace MicroShop.StockAPI.Services;

public class StockService
{
    private readonly IUnitOfWork _uow;

    public StockService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> ReserveStockAsync(Guid productId, int quantity)
    {
        var product = await _uow.Repository<Product>().GetByIdAsync(productId);

        if (product == null) return false;
        if (product.StockCount < quantity) return false;

        product.StockCount -= quantity;
        await _uow.SaveChangesAsync();

        Console.WriteLine($"[StockService] Stok düştü. Ürün: {product.Name}, Kalan: {product.StockCount}");
        return true;
    }

    public async Task ReleaseStockAsync(Guid productId, int quantity)
    {
        var product = await _uow.Repository<Product>().GetByIdAsync(productId);
        if (product != null)
        {
            product.StockCount += quantity;
            await _uow.SaveChangesAsync();
            Console.WriteLine($"[StockService] Stok İADE alındı. Ürün: {product.Name}, Yeni Stok: {product.StockCount}");
        }
    }
}