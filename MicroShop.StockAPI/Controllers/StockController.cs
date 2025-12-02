using MicroShop.Shared.Interfaces;
using MicroShop.StockAPI.Entities;
using Microsoft.AspNetCore.Mvc;

namespace MicroShop.StockAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockController  : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public StockController(IUnitOfWork uow)
    {
        _uow = uow;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = await _uow.Repository<Product>().GetAllAsync();
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var product = await _uow.Repository<Product>().GetByIdAsync(id);
        if (product == null) return NotFound();
        return Ok(product);
    }
}
