using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MicroShop.PaymentAPI.Entities;
using MicroShop.Shared.Interfaces;

namespace MicroShop.PaymentAPI.Controllers;

[Authorize] 
[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public PaymentController(IUnitOfWork uow)
    {
        _uow = uow;
    }


    [HttpGet("wallet/{userId}")]
    public async Task<IActionResult> GetWallet(Guid userId)
    {
        var wallets = await _uow.Repository<Wallet>().GetAllAsync();
        var wallet = wallets.FirstOrDefault(w => w.UserId == userId);

        if (wallet == null) return NotFound(new { Message = "Cüzdan bulunamadı." });

        return Ok(new
        {
            Balance = wallet.Balance,
            Owner = wallet.OwnerName,
            Currency = "TL"
        });
    }


    [HttpPost("wallet/deposit")]
    public async Task<IActionResult> Deposit([FromBody] DepositDto dto)
    {
        var wallets = await _uow.Repository<Wallet>().GetAllAsync();
        var wallet = wallets.FirstOrDefault(w => w.UserId == dto.UserId);

        if (wallet == null) return NotFound("Cüzdan yok.");

        wallet.Balance += dto.Amount;
        await _uow.SaveChangesAsync();

        return Ok(new { Message = "Yükleme başarılı", NewBalance = wallet.Balance });
    }

    [HttpGet("history/{userId}")]
    public async Task<IActionResult> GetHistory(Guid userId)
    {

        var logs = await _uow.Repository<PaymentLog>().GetAllAsync();

        return Ok(logs.OrderByDescending(x => x.CreatedAt).Take(10));
    }
}

// Basit DTO
public class DepositDto
{
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
}