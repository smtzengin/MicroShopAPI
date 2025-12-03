namespace MicroShop.IdentityAPI.Models;

public class RefreshTokenDto
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
}
