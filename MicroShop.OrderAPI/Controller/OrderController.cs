using MicroShop.OrderAPI.Dtos;
using MicroShop.OrderAPI.Entities;
using MicroShop.OrderAPI.Services;
using MicroShop.Shared.Interfaces;
using MicroShop.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace MicroShop.OrderAPI.Controller;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly IMessageProducer _messageProducer;

    public OrderController(IUnitOfWork uow, IMessageProducer messageProducer)
    {
        _uow = uow;
        _messageProducer = messageProducer;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        var newOrder = new Order
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            TotalPrice = dto.Price,
            Status = OrderState.Created,
            CreatedAt = DateTime.Now
        };

        await _uow.Repository<Order>().AddAsync(newOrder);
        await _uow.SaveChangesAsync();


        var sagaEvent = new SagaEvent
        {
            OrderId = newOrder.Id,
            ProductId = newOrder.ProductId,
            UserId = dto.UserId,
            Quantity = newOrder.Quantity,
            TotalPrice = newOrder.TotalPrice,
            CurrentState = OrderState.Created
        };
        _messageProducer.SendMessage(sagaEvent);

        return Ok(new { OrderId = newOrder.Id });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var order = await _uow.Repository<Order>().GetByIdAsync(id);
        if (order == null) return NotFound();

        return Ok(new
        {
            Id = order.Id,
            Status = order.Status.ToString(),
            FailReason = order.FailReason
        });
    }
}
