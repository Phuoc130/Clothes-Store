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
    [Authorize(Roles = "User,Admin")]
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
                    c.Product.ImageUrl
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> AddToWishlist([FromBody] AddCartItemRequest request)
        {
            var userId = GetUserId();

            var existed = await _context.WishlistItems
                .AnyAsync(c => c.UserId == userId && c.ProductId == request.ProductId);

            if (!existed)
            {
                _context.WishlistItems.Add(new WishlistItem
                {
                    UserId = userId,
                    ProductId = request.ProductId
                });

                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Added to wishlist" });
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
            return Ok(new { message = "Removed" });
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        }
    }
}

