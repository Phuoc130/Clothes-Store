using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ProductStore.Models
{
    public static class IdentitySeedData
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var db = services.GetRequiredService<ProductDbContext>();

            var roles = new[] { "Admin", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            const string adminEmail = "admin@gmail.com";
            const string adminPassword = "Admin123@";

            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(admin, adminPassword);
                await userManager.AddToRoleAsync(admin, "Admin");
            }

            if (!await db.ShopOrders.AnyAsync() && admin != null)
            {
                var topProducts = await db.Products.OrderByDescending(p => p.Product_ID).Take(2).ToListAsync();
                if (topProducts.Count > 0)
                {
                    var order = new ShopOrder
                    {
                        UserId = admin.Id,
                        TotalAmount = topProducts.Sum(p => p.FinalPrice),
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        Items = topProducts.Select(p => new ShopOrderItem
                        {
                            ProductId = p.Product_ID,
                            Quantity = 1,
                            UnitPrice = p.FinalPrice
                        }).ToList()
                    };

                    db.ShopOrders.Add(order);
                    await db.SaveChangesAsync();
                }
            }
        }
    }
}

