using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductStore.Models;

namespace ProductStore.Controllers.Api
{
    [ApiController]
    [Route("api/admin/products")]
    public class AdminProductsApiController : ControllerBase
    {
        private readonly ProductDbContext _context;
        private const int PageSize = 8;

        public AdminProductsApiController(ProductDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int productPage = 1, [FromQuery] string? search = null, [FromQuery] string? category = null, [FromQuery] string? stockFilter = null)
        {
            var query = _context.Products.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.Name.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p => p.Category == category);
            }

            if (stockFilter == "in")
            {
                query = query.Where(p => p.Stock > 0);
            }
            else if (stockFilter == "out")
            {
                query = query.Where(p => p.Stock <= 0);
            }

            var totalItems = await query.CountAsync();
            var products = await query
                .OrderByDescending(p => p.Product_ID)
                .Skip((Math.Max(productPage, 1) - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var categories = await _context.Products.AsNoTracking()
                .Select(p => p.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .Cast<string>()
                .OrderBy(c => c)
                .ToListAsync();

            return Ok(new
            {
                Products = products,
                Search = search,
                Category = category,
                StockFilter = stockFilter,
                Categories = categories,
                PagingInfo = new
                {
                    CurrentPage = Math.Max(productPage, 1),
                    ItemsPerPage = PageSize,
                    TotalItems = totalItems
                }
            });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Product_ID == id);
            return product == null ? NotFound() : Ok(product);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Product product)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = product.Product_ID }, product);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Product product)
        {
            if (id != product.Product_ID)
            {
                return BadRequest();
            }

            if (!await _context.Products.AnyAsync(p => p.Product_ID == id))
            {
                return NotFound();
            }

            _context.Products.Update(product);
            await _context.SaveChangesAsync();
            return Ok(product);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Deleted" });
        }
    }
}

