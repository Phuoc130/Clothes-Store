using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductStore.Contracts.Shop;
using ProductStore.Models;

namespace ProductStore.Controllers.Api
{
    [ApiController]
    [Route("api/admin")]
    public class AdminApiController : ControllerBase
    {
        private readonly ProductDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminApiController(ProductDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var totalRevenue = await _context.ShopOrders.SumAsync(o => o.TotalAmount);
            var orderCount = await _context.ShopOrders.CountAsync();
            var userCount = await _userManager.Users.CountAsync();

            var topViewedProducts = await _context.ProductViews
                .GroupBy(v => v.ProductId)
                .Select(g => new { ProductId = g.Key, Views = g.Count() })
                .OrderByDescending(x => x.Views)
                .Take(5)
                .ToListAsync();

            var productIds = topViewedProducts.Select(x => x.ProductId).ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Product_ID))
                .ToDictionaryAsync(p => p.Product_ID, p => p.Name);

            var hotProducts = topViewedProducts.Select(x => new
            {
                x.ProductId,
                Name = products.TryGetValue(x.ProductId, out var name) ? name : "Unknown",
                x.Views
            });

            return Ok(new
            {
                TotalRevenue = totalRevenue,
                OrderCount = orderCount,
                UserCount = userCount,
                TopViewedProducts = hotProducts
            });
        }

        [HttpGet("orders")]
        public async Task<IActionResult> Orders()
        {
            var orders = await _context.ShopOrders
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    o.ShopOrderId,
                    o.UserId,
                    o.TotalAmount,
                    o.Status,
                    o.CreatedAt
                })
                .ToListAsync();

            return Ok(orders);
        }

        [HttpPatch("orders/{id:int}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
        {
            var status = request.Status?.Trim();
            var allowed = new[] { "Pending", "Shipping", "Completed" };
            if (string.IsNullOrWhiteSpace(status) || !allowed.Contains(status, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest("Status must be Pending, Shipping, or Completed.");
            }

            var order = await _context.ShopOrders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = status;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Order status updated" });
        }

        [HttpGet("users")]
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.ToListAsync();
            var result = new List<object>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result.Add(new
                {
                    user.Id,
                    user.Email,
                    Roles = roles,
                    IsLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow
                });
            }

            return Ok(result);
        }

        [HttpPatch("users/{id}/lock")]
        public async Task<IActionResult> LockUser(string id, [FromQuery] bool lockAccount = true)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.LockoutEnabled = true;
            user.LockoutEnd = lockAccount ? DateTimeOffset.UtcNow.AddYears(10) : null;
            await _userManager.UpdateAsync(user);

            return Ok(new { message = lockAccount ? "User locked" : "User unlocked" });
        }

        [HttpPatch("users/{id}/role")]
        public async Task<IActionResult> ChangeRole(string id, [FromQuery] string role = "User")
        {
            if (role != "User" && role != "Admin")
            {
                return BadRequest("Role must be User or Admin");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, roles);
            }

            await _userManager.AddToRoleAsync(user, role);
            return Ok(new { message = "Role updated" });
        }
    }
}

