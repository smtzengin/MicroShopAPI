using MicroShop.OrderAPI.Dtos;
using MicroShop.OrderAPI.Entities;
using MicroShop.OrderAPI.Models;
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
            BuyerId = dto.UserId, // UserId -> BuyerId (Guid)
            Status = OrderState.Created,
            CreatedAt = DateTime.UtcNow,
            CouponCode = dto.CouponCode,
            PaymentType = dto.PaymentType,

            // Adres Eşlemesi
            ShippingAddress = new Address
            {
                Line = dto.Address.Line,
                City = dto.Address.City,
                District = dto.Address.District,
                ZipCode = dto.Address.ZipCode
            }
        };

        // 2. Ürünleri Ekle ve Toplam Tutar Hesapla
        foreach (var item in dto.Items)
        {
            newOrder.Items.Add(new OrderItem
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Price = item.Price,
                Quantity = item.Quantity,
                OrderId = newOrder.Id,

            });
        }

        // Toplam tutarı itemlardan hesapla
        newOrder.TotalPrice = newOrder.Items.Sum(x => x.Price * x.Quantity);

        // 3. Veritabanına Kaydet
        await _uow.Repository<Order>().AddAsync(newOrder);
        await _uow.SaveChangesAsync();

        // 4. SAGA EVENT OLUŞTUR (Kritik Düzeltme Burada!)
        var sagaEvent = new SagaEvent
        {
            OrderId = newOrder.Id,
            UserId = newOrder.BuyerId,
            TotalPrice = newOrder.TotalPrice,
            CurrentState = OrderState.Created,
            CouponCode = dto.CouponCode,
            PaymentType = dto.PaymentType,

            CardInfo = dto.PaymentType == PaymentType.CreditCard && dto.CardInfo != null
                ? new CreditCardInfo
                {
                    CardNumber = dto.CardInfo.CardNumber,
                    HolderName = dto.CardInfo.HolderName,
                    CVV = dto.CardInfo.CVV,
                    Expiration = dto.CardInfo.Expiration
                }
                : null,

            // Listeyi SagaEvent formatına çeviriyoruz
            Items = newOrder.Items.Select(x => new SagaOrderItem
            {
                ProductId = x.ProductId,
                Quantity = x.Quantity
            }).ToList()
        };

        // RabbitMQ'ya gönder
        _messageProducer.SendMessage(sagaEvent, "queue.stock");

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
