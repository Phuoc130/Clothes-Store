using Microsoft.AspNetCore.Authorization;
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
        private readonly IWebHostEnvironment _environment;
        private const int PageSize = 8;

        public AdminProductsApiController(ProductDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
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
        [Consumes("application/json")]
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

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateForm([FromForm] Product product, [FromForm] List<IFormFile>? imageFiles)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            ApplyUploadedImages(product, imageFiles);
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = product.Product_ID }, product);
        }

        [HttpPut("{id:int}")]
        [Consumes("application/json")]
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

        [HttpPut("{id:int}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateForm(int id, [FromForm] Product product, [FromForm] List<IFormFile>? imageFiles)
        {
            if (id != product.Product_ID)
            {
                return BadRequest();
            }

            var current = await _context.Products.FirstOrDefaultAsync(p => p.Product_ID == id);
            if (current == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            current.Name = product.Name;
            current.Description = product.Description;
            current.Price = product.Price;
            current.DiscountPercent = product.DiscountPercent;
            current.Category = product.Category;
            current.Type = product.Type;
            current.Stock = product.Stock;
            current.Availability = product.Availability;
            current.ColorHex = product.ColorHex;
            current.SizesCsv = product.SizesCsv;
            current.Material_ID = product.Material_ID;
            current.Subcontractor_ID = product.Subcontractor_ID;

            ApplyUploadedImages(current, imageFiles);

            await _context.SaveChangesAsync();
            return Ok(current);
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

        [HttpGet("images/{fileName}")]
        [AllowAnonymous]
        public IActionResult GetProductImage(string fileName)
        {
            var safeFileName = Path.GetFileName(fileName);
            if (string.IsNullOrWhiteSpace(safeFileName))
            {
                return BadRequest();
            }

            var folder = EnsureProductImageFolder();
            var fullPath = Path.Combine(folder, safeFileName);
            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound();
            }

            var ext = Path.GetExtension(fullPath).ToLowerInvariant();
            var contentType = ext switch
            {
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            return PhysicalFile(fullPath, contentType);
        }

        private string EnsureProductImageFolder()
        {
            var folder = Path.Combine(_environment.ContentRootPath, "wwwroot", "product-images");
            Directory.CreateDirectory(folder);
            return folder;
        }

        private void ApplyUploadedImages(Product product, List<IFormFile>? imageFiles)
        {
            if (imageFiles == null || imageFiles.Count == 0)
            {
                return;
            }

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var folder = EnsureProductImageFolder();
            var uploadedUrls = new List<string>();

            foreach (var file in imageFiles.Where(f => f != null && f.Length > 0))
            {
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowed.Contains(ext))
                {
                    continue;
                }

                var fileName = $"prd-{Guid.NewGuid():N}{ext}";
                var fullPath = Path.Combine(folder, fileName);

                using var stream = new FileStream(fullPath, FileMode.CreateNew);
                file.CopyTo(stream);
                uploadedUrls.Add($"/api/admin/products/images/{fileName}");
            }

            if (uploadedUrls.Count == 0)
            {
                return;
            }

            product.ImageUrl = uploadedUrls[0];
            product.HoverImageUrl = uploadedUrls.Count > 1 ? uploadedUrls[1] : uploadedUrls[0];
            product.AdditionalImageUrls = uploadedUrls.Count > 2
                ? string.Join(';', uploadedUrls.Skip(2))
                : null;
        }
    }
}

