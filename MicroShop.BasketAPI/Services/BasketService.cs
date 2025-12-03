using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using MicroShop.BasketAPI.Models;

namespace MicroShop.BasketAPI.Services;

public class BasketService
{
    private readonly IDistributedCache _redisCache;

    public BasketService(IDistributedCache redisCache)
    {
        _redisCache = redisCache;
    }

    // 1. Sepeti Getir 
    public async Task<ShoppingCart> GetBasketAsync(Guid userId)
    {
        var basketJson = await _redisCache.GetStringAsync(userId.ToString());
        return string.IsNullOrEmpty(basketJson)
            ? new ShoppingCart(userId) // Sepet yoksa boş dön
            : JsonSerializer.Deserialize<ShoppingCart>(basketJson);
    }

    // 2. Sepete Ürün Ekle (AKILLI MANTIK)
    public async Task<ShoppingCart> AddItemToBasketAsync(Guid userId, BasketItem newItem)
    {
        // Önce mevcut sepeti çek
        var basket = await GetBasketAsync(userId);

        // Ürün zaten var mı?
        var existingItem = basket.Items.FirstOrDefault(x => x.ProductId == newItem.ProductId);

        if (existingItem != null)
        {
            // Varsa adedini artır
            existingItem.Quantity += newItem.Quantity;
        }
        else
        {
            // Yoksa listeye ekle
            basket.Items.Add(newItem);
        }

        // Güncel halini Redis'e kaydet
        return await SaveBasketAsync(basket);
    }

    // 3. Sepetten Ürün Sil / Adet Düşür
    public async Task<ShoppingCart> RemoveItemFromBasketAsync(Guid userId, Guid productId)
    {
        var basket = await GetBasketAsync(userId);
        var existingItem = basket.Items.FirstOrDefault(x => x.ProductId == productId);

        if (existingItem == null)
            return basket;

        // Adet 1'den büyükse azalt, 1 ise tamamen sil
        if (existingItem.Quantity > 1)
        {
            existingItem.Quantity--;
        }
        else
        {
            basket.Items.Remove(existingItem);
        }

        return await SaveBasketAsync(basket);
    }

    // 4. Sepeti Komple Sil 
    public async Task DeleteBasketAsync(Guid userId)
    {
        await _redisCache.RemoveAsync(userId.ToString());
    }

    // --- Yardımcı Metot: Redis'e Yazma ---
    private async Task<ShoppingCart> SaveBasketAsync(ShoppingCart basket)
    {
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var json = JsonSerializer.Serialize(basket, jsonOptions);

        var options = new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromDays(7)
        };

        await _redisCache.SetStringAsync(basket.UserId.ToString(), json, options);

        return basket;
    }
}