using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductStore.Contracts.Shop;
using ProductStore.Models;

namespace ProductStore.Controllers.Api
{
    [ApiController]
    [Route("api/wishlist")]
    [Authorize]
    public class WishlistApiController : ControllerBase
    {
        private readonly ProductDbContext _context;

        public WishlistApiController(ProductDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyWishlist()
        {
            var userId = GetUserId();
            var items = await _context.WishlistItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .Select(c => new
                {
                    c.ProductId,
                    ProductName = c.Product!.Name,
                    Price = c.Product.FinalPrice,
                    c.Product.ImageUrl,
                    c.Product.Stock,
                    c.Product.Availability,
                    c.CreatedAt
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetWishlistCount()
        {
            var userId = GetUserId();
            var count = await _context.WishlistItems
                .Where(c => c.UserId == userId)
                .CountAsync();

            return Ok(new { count });
        }

        [HttpPost]
        public async Task<IActionResult> AddToWishlist([FromBody] AddCartItemRequest request)
        {
            var userId = GetUserId();
            System.Console.WriteLine($"[WishlistAPI] AddToWishlist: userId={userId}, productId={request.ProductId}");

            if (string.IsNullOrWhiteSpace(userId))
            {
                System.Console.WriteLine($"[WishlistAPI] ERROR: userId is empty or whitespace!");
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập để thêm vào yêu thích" });
            }

            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Product_ID == request.ProductId);

            if (product == null)
            {
                System.Console.WriteLine($"[WishlistAPI] Product {request.ProductId} not found!");
                return NotFound(new { success = false, message = "Sản phẩm không tồn tại" });
            }

            var existed = await _context.WishlistItems
                .AnyAsync(c => c.UserId == userId && c.ProductId == request.ProductId);

            if (existed)
            {
                var existCount = await _context.WishlistItems
                    .Where(c => c.UserId == userId)
                    .CountAsync();

                System.Console.WriteLine($"[WishlistAPI] Item already exists");
                return Ok(new { success = true, alreadyExisted = true, message = "Sản phẩm đã có trong yêu thích", count = existCount });
            }

            System.Console.WriteLine($"[WishlistAPI] Creating new wishlist item");
            _context.WishlistItems.Add(new WishlistItem
            {
                UserId = userId,
                ProductId = request.ProductId
            });

            await _context.SaveChangesAsync();
            System.Console.WriteLine($"[WishlistAPI] Saved to database");

            var count = await _context.WishlistItems
                .Where(c => c.UserId == userId)
                .CountAsync();

            System.Console.WriteLine($"[WishlistAPI] Total count: {count}");
            return Ok(new { success = true, alreadyExisted = false, message = "Đã thêm vào yêu thích", count });
        }

        [HttpDelete("{productId:int}")]
        public async Task<IActionResult> RemoveFromWishlist(int productId)
        {
            var userId = GetUserId();
            var item = await _context.WishlistItems.FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);
            if (item == null)
            {
                return NotFound();
            }

            _context.WishlistItems.Remove(item);
            await _context.SaveChangesAsync();

            var count = await _context.WishlistItems
                .Where(c => c.UserId == userId)
                .CountAsync();

            return Ok(new { success = true, message = "Đã xóa khỏi yêu thích", count });
        }

        private string GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            System.Console.WriteLine($"[WishlistAPI.GetUserId] userId={userId}, User claims count={User.Claims.Count()}");
            foreach (var claim in User.Claims.Take(5))
            {
                System.Console.WriteLine($"  - {claim.Type}: {claim.Value[..Math.Min(50, claim.Value.Length)]}");
            }
            return userId;
        }
    }
}
