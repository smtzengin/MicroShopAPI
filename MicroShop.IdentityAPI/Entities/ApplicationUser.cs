using Microsoft.AspNetCore.Identity;

namespace MicroShop.IdentityAPI.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
}