using MicroShop.IdentityAPI.Entities;
using MicroShop.IdentityAPI.Models;
using MicroShop.Shared.Interfaces;
using MicroShop.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MicroShop.IdentityAPI.Services;

public class AuthService(UserManager<ApplicationUser> userManager,
                   SignInManager<ApplicationUser> signInManager,
                   IConfiguration configuration)
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
    private readonly IConfiguration _configuration = configuration;
    private readonly IMessageProducer _messageProducer;

    // --- 1. LOGIN (GÜNCELLENDİ) ---
    public async Task<AuthResponseDto> LoginAsync(LoginDto model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
            return new AuthResponseDto { IsSuccess = false, ErrorMessage = "Kullanıcı bulunamadı" };

        var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
        if (!result.Succeeded)
            return new AuthResponseDto { IsSuccess = false, ErrorMessage = "Şifre hatalı" };

        // Access Token Üret
        var accessToken = GenerateJwtToken(user);

        // Refresh Token Üret
        var refreshToken = GenerateRefreshToken();

        // Refresh Token'ı DB'ye kaydet (Örn: 7 gün geçerli)
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(user);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            IsSuccess = true
        };
    }

    public async Task<(bool IsSuccess, string Error)> RegisterAsync(RegisterDto model)
    {
        var user = new ApplicationUser
        {
            UserName = model.UserName,
            Email = model.Email,
            FullName = model.FullName
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            var eventMessage = new UserCreatedEvent
            {
                UserId = Guid.Parse(user.Id), // Identity string tutar, biz Guid'e çeviriyoruz
                Email = user.Email,
                FullName = user.FullName
            };
            _messageProducer.SendMessage(eventMessage, "queue.identity.user-created");
            return (true, null);
        }

        var error = string.Join(", ", result.Errors.Select(e => e.Description));
        return (false, error);
    }

    // --- 2. TOKEN YENİLEME (REFRESH) ---
    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto model)
    {
        // 1. Access Token'ın içinden "User" bilgisini (Principal) çıkar (Süresi bitmiş olsa bile)
        var principal = GetPrincipalFromExpiredToken(model.AccessToken);
        if (principal == null)
            return new AuthResponseDto { IsSuccess = false, ErrorMessage = "Geçersiz Access Token" };

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.FindByIdAsync(userId);

        // 2. Kontroller
        if (user == null || user.RefreshToken != model.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            return new AuthResponseDto { IsSuccess = false, ErrorMessage = "Geçersiz veya süresi dolmuş Refresh Token" };
        }

        // 3. Yeni Tokenları Üret (Rotation: Refresh Token da değişir güvenlik için)
        var newAccessToken = GenerateJwtToken(user);
        var newRefreshToken = GenerateRefreshToken();

        // 4. DB'yi Güncelle
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // Süreyi uzat
        await _userManager.UpdateAsync(user);

        return new AuthResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            IsSuccess = true
        };
    }

    // --- YARDIMCI METOTLAR ---

    private string GenerateJwtToken(ApplicationUser user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            // Access Token ömrü kısadır (Örn: 15-60 dk)
            Expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["DurationInMinutes"])),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }

    // Süresi bitmiş token'dan kullanıcıyı bulmak için
    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]);

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateLifetime = false // ÖNEMLİ: Süresi bitmiş olsa bile validate et diyoruz
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            return null; // Yanlış algoritma vs.
        }

        return principal;
    }
}