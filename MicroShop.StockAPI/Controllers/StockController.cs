using MicroShop.Shared.Interfaces;
using MicroShop.Shared.Models;
using MicroShop.StockAPI.Entities;
using MicroShop.StockAPI.Models;
using MicroShop.StockAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace MicroShop.StockAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockController(IUnitOfWork uow, StockService stockService) : ControllerBase
{
    private readonly IUnitOfWork _uow = uow;
    private readonly StockService _stockService = stockService;

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
}
