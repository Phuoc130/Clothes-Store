using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductStore.Contracts.Shop;
using ProductStore.Models;

namespace ProductStore.Controllers.Api
{
    [ApiController]
    [Route("api/cart")]
    [Authorize]
    public class CartApiController : ControllerBase
    {
        private readonly ProductDbContext _context;

        public CartApiController(ProductDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyCart()
        {
            var userId = GetUserId();
            var items = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .Select(c => new
                {
                    c.ProductId,
                    c.Quantity,
                    c.SelectedSize,
                    ProductName = c.Product!.Name,
                    Price = c.Product.FinalPrice,
                    c.Product.ImageUrl,
                    c.Product.Stock,
                    c.Product.Availability
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetCartCount()
        {
            var userId = GetUserId();
            var count = await _context.CartItems
                .Where(c => c.UserId == userId)
                .SumAsync(c => c.Quantity);

            return Ok(new { count });
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] AddCartItemRequest request)
        {
            var userId = GetUserId();
            System.Console.WriteLine($"[CartAPI] AddToCart: userId={userId}, productId={request.ProductId}, qty={request.Quantity}, size={request.Size}");

            if (string.IsNullOrWhiteSpace(userId))
            {
                System.Console.WriteLine($"[CartAPI] ERROR: userId is empty or whitespace!");
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập để thêm vào giỏ hàng" });
            }

            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Product_ID == request.ProductId);

            if (product == null)
            {
                System.Console.WriteLine($"[CartAPI] Product {request.ProductId} not found!");
                return NotFound(new { success = false, message = "Sản phẩm không tồn tại" });
            }

            System.Console.WriteLine($"[CartAPI] Product found: {product.Name}, Availability={product.Availability}, Stock={product.Stock}");

            if (!product.Availability)
            {
                return BadRequest(new { success = false, message = "Sản phẩm không khả dụng" });
            }

            if (product.Stock <= 0)
            {
                return BadRequest(new { success = false, message = "Sản phẩm hết hàng" });
            }

            var qty = Math.Max(request.Quantity, 1);
            var size = request.Size?.Trim();

            // Find existing item with same product AND same size
            var existing = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId
                    && c.ProductId == request.ProductId
                    && c.SelectedSize == size);

            if (existing != null)
            {
                System.Console.WriteLine($"[CartAPI] Item exists, updating quantity from {existing.Quantity} to {existing.Quantity + qty}");
                existing.Quantity += qty;
            }
            else
            {
                System.Console.WriteLine($"[CartAPI] Creating new cart item");
                _context.CartItems.Add(new CartItem
                {
                    UserId = userId,
                    ProductId = request.ProductId,
                    Quantity = qty,
                    SelectedSize = size
                });
            }

            await _context.SaveChangesAsync();
            System.Console.WriteLine($"[CartAPI] Saved to database");

            var count = await _context.CartItems
                .Where(c => c.UserId == userId)
                .SumAsync(c => c.Quantity);

            System.Console.WriteLine($"[CartAPI] Total cart count: {count}");
            return Ok(new { success = true, message = "Thêm vào giỏ hàng thành công", count });
        }

        [HttpDelete("{productId:int}")]
        public async Task<IActionResult> RemoveFromCart(int productId, [FromQuery] string? size = null)
        {
            var userId = GetUserId();
            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId
                    && c.ProductId == productId
                    && c.SelectedSize == size);
            if (item == null)
            {
                // Fallback: remove any item with this productId
                item = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);
            }
            if (item == null)
            {
                return NotFound();
            }

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();

            var count = await _context.CartItems
                .Where(c => c.UserId == userId)
                .SumAsync(c => c.Quantity);

            return Ok(new { success = true, message = "Đã xóa khỏi giỏ hàng", count });
        }

        [HttpPut("{productId:int}")]
        public async Task<IActionResult> UpdateQuantity(int productId, [FromBody] UpdateCartQuantityRequest request)
        {
            var userId = GetUserId();
            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

            if (item == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ" });
            }

            if (request.Quantity <= 0)
            {
                _context.CartItems.Remove(item);
            }
            else
            {
                item.Quantity = Math.Min(request.Quantity, 99);
            }

            await _context.SaveChangesAsync();

            var count = await _context.CartItems
                .Where(c => c.UserId == userId)
                .SumAsync(c => c.Quantity);

            return Ok(new { success = true, message = "Cập nhật giỏ hàng thành công", count });
        }

        private string GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            System.Console.WriteLine($"[CartAPI.GetUserId] userId={userId}, User claims count={User.Claims.Count()}");
            foreach (var claim in User.Claims.Take(5))
            {
                System.Console.WriteLine($"  - {claim.Type}: {claim.Value[..Math.Min(50, claim.Value.Length)]}");
            }
            return userId;
        }
    }
}
