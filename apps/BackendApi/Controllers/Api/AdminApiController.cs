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
        private readonly IWebHostEnvironment _environment;

        public AdminApiController(ProductDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var paidStatuses = new[] { "Paid", "Completed" };
            var paidOrders = _context.ShopOrders.Where(o => paidStatuses.Contains(o.Status));

            var totalRevenue = await paidOrders.SumAsync(o => o.TotalAmount);
            var orderCount = await _context.ShopOrders.CountAsync();
            var userCount = await _userManager.Users.CountAsync();

            var startDate = DateTime.UtcNow.Date.AddDays(-6);
            var revenueByDayRaw = await paidOrders
                .Where(o => o.CreatedAt >= startDate)
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(x => x.TotalAmount) })
                .ToListAsync();

            var revenueByDay = Enumerable.Range(0, 7)
                .Select(offset => startDate.AddDays(offset))
                .Select(day => new
                {
                    Date = day.ToString("dd/MM"),
                    Total = revenueByDayRaw.FirstOrDefault(x => x.Date == day)?.Total ?? 0m
                })
                .ToList();

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
                TopViewedProducts = hotProducts,
                RevenueSeries = revenueByDay
            });
        }

        [HttpGet("orders")]
        public async Task<IActionResult> Orders()
        {
            var entities = await _context.ShopOrders
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var userIds = entities.Select(o => o.UserId).Distinct().ToList();
            var userMap = await _userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Email ?? string.Empty);

            var orders = entities.Select(o => new
            {
                o.ShopOrderId,
                o.UserId,
                UserEmail = userMap.TryGetValue(o.UserId, out var email) ? email : "Unknown",
                o.TotalAmount,
                o.Status,
                o.CreatedAt,
                HasTransferProof = HasTransferProof(o.ShopOrderId),
                TransferProofUrl = GetTransferProofUrl(o.ShopOrderId)
            });

            return Ok(orders);
        }

        [HttpPatch("orders/{id:int}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
        {
            var status = request.Status?.Trim();
            var allowed = new[] { "Pending", "PendingPayment", "PendingTransfer", "Paid", "PaymentFailed", "Shipping", "Completed", "Cancelled" };
            if (string.IsNullOrWhiteSpace(status) || !allowed.Contains(status, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest("Status must be Pending, PendingPayment, PendingTransfer, Paid, PaymentFailed, Shipping, Completed, or Cancelled.");
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

        private string EnsureProofFolder()
        {
            var folder = Path.Combine(_environment.ContentRootPath, "wwwroot", "payment-proofs");
            Directory.CreateDirectory(folder);
            return folder;
        }

        private bool HasTransferProof(int orderId)
        {
            var folder = EnsureProofFolder();
            var pattern = $"order-{orderId}-*.*";
            return Directory.GetFiles(folder, pattern).Length > 0;
        }

        private string? GetTransferProofUrl(int orderId)
        {
            var folder = EnsureProofFolder();
            var pattern = $"order-{orderId}-*.*";
            var file = Directory.GetFiles(folder, pattern).OrderByDescending(f => f).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(file))
            {
                return null;
            }

            return $"/api/orders/proof/{orderId}";
        }
    }
}

