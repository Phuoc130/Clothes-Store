using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductStore.Models;

namespace ProductStore.Controllers.Api
{
    [ApiController]
    [Route("api/public/products")]
    public class PublicProductsApiController : ControllerBase
    {
        private readonly ProductDbContext _context;
        private static readonly string[] DefaultSizes = ["S", "M", "L", "XL"];

        public PublicProductsApiController(ProductDbContext context)
        {
            _context = context;
        }

        [HttpGet("featured")]
        public async Task<IActionResult> Featured([FromQuery] int take = 8)
        {
            var products = await _context.Products
                .AsNoTracking()
                .Where(p => p.Availability)
                .OrderByDescending(p => p.Product_ID)
                .Take(Math.Clamp(take, 1, 24))
                .ToListAsync();

            return Ok(products);
        }

        [HttpGet("shop")]
        public async Task<IActionResult> Shop(
            [FromQuery] string? search,
            [FromQuery] string? category,
            [FromQuery] string? size,
            [FromQuery] string? color,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] string sort = "newest",
            [FromQuery] int load = 8)
        {
            var query = _context.Products
                .AsNoTracking()
                .Where(p => p.Availability)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.Name.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p => p.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(size))
            {
                query = query.Where(p => p.SizesCsv != null && p.SizesCsv.Contains(size));
            }

            if (!string.IsNullOrWhiteSpace(color))
            {
                query = query.Where(p => p.ColorHex == color);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            query = sort switch
            {
                "price-asc" => query.OrderBy(p => p.Price),
                "price-desc" => query.OrderByDescending(p => p.Price),
                "newest" => query.OrderByDescending(p => p.Product_ID),
                _ => query.OrderByDescending(p => p.Product_ID)
            };

            var totalCount = await query.CountAsync();
            var products = await query.Take(Math.Max(load, 8)).ToListAsync();
            var allProducts = await _context.Products.AsNoTracking().ToListAsync();

            var categories = allProducts
                .Select(p => p.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .Cast<string>()
                .ToList();

            var sizes = allProducts
                .SelectMany(p => p.GetSizes())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToList();

            var colors = allProducts
                .Select(p => p.ColorHex)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .Cast<string>()
                .ToList();

            if (sizes.Count == 0)
            {
                sizes = DefaultSizes.ToList();
            }

            return Ok(new
            {
                Products = products,
                Categories = categories,
                Sizes = sizes,
                Colors = colors,
                Search = search,
                Category = category,
                Size = size,
                Color = color,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                Sort = sort,
                LoadCount = Math.Max(load, 8),
                TotalCount = totalCount,
                HasMore = Math.Max(load, 8) < totalCount
            });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Product_ID == id);
            if (product == null)
            {
                return NotFound();
            }

            var relatedProducts = await _context.Products.AsNoTracking()
                .Where(p => p.Product_ID != product.Product_ID && p.Category == product.Category)
                .OrderByDescending(p => p.Product_ID)
                .Take(4)
                .ToListAsync();

            _context.ProductViews.Add(new ProductView
            {
                ProductId = product.Product_ID,
                ViewedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Product = product,
                GalleryImages = product.GetImageGallery(),
                RelatedProducts = relatedProducts
            });
        }
    }
}

