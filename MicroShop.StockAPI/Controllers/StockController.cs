using MicroShop.Shared.Interfaces;
using MicroShop.Shared.Models;
using MicroShop.StockAPI.Entities;
using MicroShop.StockAPI.Models;
using MicroShop.StockAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroShop.StockAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockController(IUnitOfWork uow, StockService stockService, IMessageProducer messageProducer) : ControllerBase
{
    private readonly IUnitOfWork _uow = uow;
    private readonly StockService _stockService = stockService;
    private readonly IMessageProducer _messageProducer = messageProducer;

    [HttpGet]
    public async Task<ActionResult<PagedResponse<Product>>> GetAll([FromQuery] ProductFilterParams filter)
    {
        var response = await _stockService.GetProductsAsync(filter);
        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var product = await _uow.Repository<Product>().GetByIdAsync(id);
        if (product == null)
            return NotFound();
        return Ok(product);
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _uow.Repository<Category>().GetAllAsync();
        return Ok(categories);
    }
    [Authorize(Roles = "Seller")] 
    [HttpGet("my-products")]
    public async Task<IActionResult> GetMyProducts([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return Unauthorized();

        var sellerId = Guid.Parse(userIdClaim.Value);

        var filter = new ProductFilterParams
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SellerId = sellerId, 
            OnlyApproved = false 
        };

        var response = await _stockService.GetProductsAsync(filter);
        return Ok(response);
    }
}
