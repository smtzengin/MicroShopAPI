using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicroShop.OrderAPI.Data;
using MicroShop.Shared.Models; // OrderState için

namespace MicroShop.OrderAPI.Controllers;

[Authorize(Roles = "Seller")]
[ApiController]
[Route("api/[controller]")]
public class SellerController : ControllerBase
{
    private readonly OrderDbContext _context;

    public SellerController(OrderDbContext context)
    {
        _context = context;
    }

    // GET: api/seller/stats
    [HttpGet("stats")]
    public async Task<IActionResult> GetSalesStats()
    {
        // 1. Token'dan Satıcı ID'yi bul
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return Unauthorized();
        var sellerId = Guid.Parse(userIdClaim.Value);

        // 2. Bu satıcıya ait, TAMAMLANMIŞ sipariş satırlarını bul
        var salesData = await _context.OrderItems
            .Include(i => i.Order)
            .Where(i => i.SellerId == sellerId && i.Order.Status == OrderState.Completed)
            .ToListAsync();

        // 3. İstatistikleri Hesapla
        var stats = new
        {
            TotalRevenue = salesData.Sum(x => x.Price * x.Quantity), // Toplam Ciro
            TotalItemsSold = salesData.Sum(x => x.Quantity),         // Toplam Satılan Adet
            TotalOrders = salesData.Select(x => x.OrderId).Distinct().Count(), // Kaç farklı sipariş

            // Son 5 Satış Hareketi
            RecentSales = salesData
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .Select(x => new
                {
                    ProductName = x.ProductName,
                    Quantity = x.Quantity,
                    Price = x.Price,
                    Date = x.CreatedAt,
                    BuyerId = x.Order.BuyerId
                })
        };

        return Ok(stats);
    }
}