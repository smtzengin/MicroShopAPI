namespace MicroShop.IdentityAPI.Models;

public class RegisterDto
{
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Role { get; set; } = "Customer";
    public string Password { get; set; }
    public string UserName { get; set; }
}