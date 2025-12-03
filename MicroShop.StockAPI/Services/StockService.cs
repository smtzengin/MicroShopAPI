using MicroShop.Shared.Interfaces;
using MicroShop.Shared.Models;
using MicroShop.StockAPI.Entities;
using MicroShop.StockAPI.Models;
using System.Linq.Expressions;

namespace MicroShop.StockAPI.Services;

public class StockService
{
    private readonly IUnitOfWork _uow;

    public StockService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        product.Status = ProductStatus.Pending; // Başlangıç durumu
        product.CreatedAt = DateTime.UtcNow;

        await _uow.Repository<Product>().AddAsync(product);
        await _uow.SaveChangesAsync();

        return product;
    }

    public async Task ApproveProductAsync(Guid productId)
    {
        var product = await _uow.Repository<Product>().GetByIdAsync(productId);
        if (product != null)
        {
            product.Status = ProductStatus.Approved;
            await _uow.SaveChangesAsync();
        }
    }


    public async Task<bool> ReserveStockAsync(List<SagaOrderItem> items)
    {

        foreach (var item in items)
        {
            var product = await _uow.Repository<Product>().GetByIdAsync(item.ProductId);
            if (product == null || product.StockCount < item.Quantity)
            {
                Console.WriteLine($"[StockService] Stok Yetersiz: {item.ProductId}");
                return false; 
            }
        }

        foreach (var item in items)
        {
            var product = await _uow.Repository<Product>().GetByIdAsync(item.ProductId);
            product.StockCount -= item.Quantity;
        }

        await _uow.SaveChangesAsync(); 
        Console.WriteLine("[StockService] Toplu stok düşüldü.");
        return true;
    }

    public async Task ReleaseStockAsync(List<SagaOrderItem> items)
    {
        foreach (var item in items)
        {
            var product = await _uow.Repository<Product>().GetByIdAsync(item.ProductId);
            if (product != null)
            {
                product.StockCount += item.Quantity;
            }
        }
        await _uow.SaveChangesAsync();
        Console.WriteLine("[StockService] Toplu stok iade edildi.");
    }

    public async Task<PagedResponse<Product>> GetProductsAsync(ProductFilterParams filter)
    {
        Expression<Func<Product, bool>> predicate = p =>
            (string.IsNullOrEmpty(filter.Search) || p.Name.Contains(filter.Search)) &&
            (!filter.CategoryId.HasValue || p.CategoryId == filter.CategoryId) &&
            (!filter.MinPrice.HasValue || p.Price >= filter.MinPrice) &&
            (!filter.MaxPrice.HasValue || p.Price <= filter.MaxPrice) &&
            (!filter.SellerId.HasValue || p.SellerId == filter.SellerId) &&
            (!filter.OnlyApproved || p.Status == ProductStatus.Approved);

        var result = await _uow.Repository<Product>()
         .GetAllPagedAsync(filter.PageNumber, filter.PageSize, predicate);

        return new PagedResponse<Product>(result.Data, filter.PageNumber, filter.PageSize, result.TotalCount);
    }
}