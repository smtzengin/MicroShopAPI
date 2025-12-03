using Microsoft.AspNetCore.Identity;
using MicroShop.IdentityAPI.Entities;

namespace MicroShop.IdentityAPI.Data;

public static class IdentityDbSeed
{
    public static async Task SeedAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        string[] roles = { "Customer", "Seller", "Admin" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var sellerEmail = "satici@magaza.com";
        if (await userManager.FindByEmailAsync(sellerEmail) == null)
        {
            var seller = new ApplicationUser
            {
                UserName = "magaza1",
                Email = sellerEmail,
                FullName = "Mega Teknoloji Mağazası",
                EmailConfirmed = true
            };

            await userManager.CreateAsync(seller, "Seller123!");
            await userManager.AddToRoleAsync(seller, "Seller");
        }
    }
}