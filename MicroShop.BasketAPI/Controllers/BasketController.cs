using MicroShop.BasketAPI.Models;
using MicroShop.BasketAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroShop.BasketAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BasketController : ControllerBase
{
    private readonly BasketService _basketService;

    public BasketController(BasketService basketService)
    {
        _basketService = basketService;
    }

    // GET: Sepeti Getir
    [HttpGet("{userId}")]
    public async Task<ActionResult<ShoppingCart>> GetBasket(Guid userId)
    {
        return Ok(await _basketService.GetBasketAsync(userId));
    }

    // POST: Ürün Ekle (Adet artırır veya yeni ekler)
    [HttpPost("{userId}/items")]
    public async Task<ActionResult<ShoppingCart>> AddItemToBasket(Guid userId, [FromBody] BasketItem item)
    {
        // Burada item.Quantity genelde 1 gelir ama frontend 5 de gönderebilir.
        var updatedBasket = await _basketService.AddItemToBasketAsync(userId, item);
        return Ok(updatedBasket);
    }

    // DELETE: Ürün Azalt / Sil
    [HttpDelete("{userId}/items/{productId}")]
    public async Task<ActionResult<ShoppingCart>> RemoveItemFromBasket(Guid userId, Guid productId)
    {
        var updatedBasket = await _basketService.RemoveItemFromBasketAsync(userId, productId);
        return Ok(updatedBasket);
    }

    // DELETE: Sepeti Komple Boşalt
    [HttpDelete("{userId}")]
    public async Task<IActionResult> DeleteBasket(Guid userId)
    {
        await _basketService.DeleteBasketAsync(userId);
        return Ok();
    }
}