namespace MicroShop.BasketAPI.Models;

public class ShoppingCart
{
    public Guid UserId { get; set; } // Sepetin sahibi (Redis Key olacak)
    public List<BasketItem> Items { get; set; } = new List<BasketItem>();

    public decimal TotalPrice => Items.Sum(x => x.Price * x.Quantity);

    // Boş constructor (Redis için gerekli)
    public ShoppingCart() { }

    public ShoppingCart(Guid userId)
    {
        UserId = userId;
    }
}
