namespace MicroShop.OrderAPI.Dtos;

public class CreditCardDto
{
    public string CardNumber { get; set; }
    public string HolderName { get; set; }
    public string Expiration { get; set; }
    public string CVV { get; set; }
}
