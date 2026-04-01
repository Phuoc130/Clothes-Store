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
    [Authorize(Roles = "User,Admin")]
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
                    ProductName = c.Product!.Name,
                    Price = c.Product.FinalPrice,
                    c.Product.ImageUrl
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] AddCartItemRequest request)
        {
            var userId = GetUserId();

            var existing = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == request.ProductId);

            if (existing != null)
            {
                existing.Quantity += Math.Max(request.Quantity, 1);
            }
            else
            {
                _context.CartItems.Add(new CartItem
                {
                    UserId = userId,
                    ProductId = request.ProductId,
                    Quantity = Math.Max(request.Quantity, 1)
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Added to cart" });
        }

        [HttpDelete("{productId:int}")]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            var userId = GetUserId();
            var item = await _context.CartItems.FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);
            if (item == null)
            {
                return NotFound();
            }

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Removed" });
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        }
    }
}

