using MicroShop.IdentityAPI.Models;
using MicroShop.IdentityAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace MicroShop.IdentityAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto model)
    {
        var result = await _authService.RegisterAsync(model);
        if (result.IsSuccess)
        {
            return Ok(new { Message = "Kullanıcı başarıyla oluşturuldu." });
        }
        return BadRequest(new { Message = result.Error });
    }
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        var result = await _authService.LoginAsync(model);
        if (!result.IsSuccess)
        {
            return Unauthorized(new { Message = result.ErrorMessage });
        }

        return Ok(result);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto model)
    {
        var result = await _authService.RefreshTokenAsync(model);
        if (!result.IsSuccess)
        {
            return BadRequest(new { Message = result.ErrorMessage });
        }
        return Ok(result);
    }
}
