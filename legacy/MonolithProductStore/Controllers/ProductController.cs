using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ProductStore.Models;
using ProductStore.Views.ViewModels;

namespace ProductStore.Controllers
{
    public class ProductController : Controller
    {
        private readonly ProductDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private const int AdminPageSize = 8;
        private static readonly string[] DefaultSizes = ["S", "M", "L", "XL"];

        public ProductController(ProductDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Shop));
        }

        public async Task<IActionResult> Shop(
            string? category,
            string? size,
            string? color,
            decimal? minPrice,
            decimal? maxPrice,
            string sort = "newest",
            int load = 8)
        {
            var query = _context.Products
                .AsNoTracking()
                .Where(p => p.Availability)
                .AsQueryable();

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
            var products = await query
                .Take(Math.Max(load, 8))
                .ToListAsync();

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

            return View(new ProductShopViewModel
            {
                Products = products,
                Categories = categories,
                Sizes = sizes,
                Colors = colors,
                Category = category,
                Size = size,
                Color = color,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                Sort = sort,
                LoadCount = Math.Max(load, 8),
                TotalCount = totalCount
            });
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(m => m.Product_ID == id);
            if (product == null)
            {
                return NotFound();
            }

            await TrackProductViewAsync(product.Product_ID);

            var relatedProducts = await _context.Products.AsNoTracking()
                .Where(p => p.Product_ID != product.Product_ID && p.Category == product.Category)
                .OrderByDescending(p => p.Product_ID)
                .Take(4)
                .ToListAsync();

            var model = new ProductDetailViewModel
            {
                Product = product,
                GalleryImages = product.GetImageGallery(),
                RelatedProducts = relatedProducts
            };

            return View(model);
        }

        public async Task<IActionResult> Admin(int productPage = 1, string? search = null, string? category = null, string? stockFilter = null)
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
                .Skip((productPage - 1) * AdminPageSize)
                .Take(AdminPageSize)
                .ToListAsync();

            var categories = await _context.Products.AsNoTracking()
                .Select(p => p.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .Cast<string>()
                .OrderBy(c => c)
                .ToListAsync();

            var model = new ProductAdminListViewModel
            {
                Products = products,
                Search = search,
                Category = category,
                StockFilter = stockFilter,
                Categories = categories,
                PagingInfo = new PagingInfo
                {
                    CurrentPage = productPage,
                    ItemsPerPage = AdminPageSize,
                    TotalItems = totalItems
                }
            };

            return View(model);
        }

        public IActionResult Create()
        {
            return View(new Product
            {
                Availability = true,
                Stock = 1,
                Price = 100000,
                Category = "Collection",
                ColorHex = "#222222",
                SizesCsv = "M,L"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, List<IFormFile>? imageFiles, string[]? selectedSizes)
        {
            product.SizesCsv = selectedSizes == null
                ? product.SizesCsv
                : string.Join(',', selectedSizes.Where(s => !string.IsNullOrWhiteSpace(s)));

            if ((imageFiles == null || imageFiles.Count == 0) && string.IsNullOrWhiteSpace(product.ImageUrl))
            {
                ModelState.AddModelError(nameof(product.ImageUrl), "Upload at least one product image from your computer.");
            }

            if (!ModelState.IsValid)
            {
                return View(product);
            }

            await SaveUploadedImagesAsync(product, imageFiles, preserveExisting: false);

            _context.Add(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Admin));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, List<IFormFile>? imageFiles, string[]? selectedSizes)
        {
            if (id != product.Product_ID)
            {
                return NotFound();
            }

            var existingProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Product_ID == id);
            if (existingProduct == null)
            {
                return NotFound();
            }

            product.ImageUrl = existingProduct.ImageUrl;
            product.HoverImageUrl = existingProduct.HoverImageUrl;
            product.AdditionalImageUrls = existingProduct.AdditionalImageUrls;

            product.SizesCsv = selectedSizes == null
                ? product.SizesCsv
                : string.Join(',', selectedSizes.Where(s => !string.IsNullOrWhiteSpace(s)));

            if (!ModelState.IsValid)
            {
                return View(product);
            }

            try
            {
                await SaveUploadedImagesAsync(product, imageFiles, preserveExisting: true);
                _context.Update(product);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(product.Product_ID))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(Admin));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FirstOrDefaultAsync(m => m.Product_ID == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Admin));
        }

        private async Task SaveUploadedImagesAsync(Product product, List<IFormFile>? imageFiles, bool preserveExisting)
        {
            if (imageFiles == null || imageFiles.Count == 0)
            {
                return;
            }

            var uploadRoot = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadRoot);

            var savedPaths = new List<string>();
            foreach (var file in imageFiles.Where(f => f.Length > 0))
            {
                if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var extension = Path.GetExtension(file.FileName);
                var fileName = $"{Guid.NewGuid():N}{extension}";
                var fullPath = Path.Combine(uploadRoot, fileName);

                await using var stream = new FileStream(fullPath, FileMode.Create);
                await file.CopyToAsync(stream);

                savedPaths.Add($"/uploads/{fileName}");
            }

            if (savedPaths.Count == 0)
            {
                return;
            }

            if (!preserveExisting || string.IsNullOrWhiteSpace(product.ImageUrl))
            {
                product.ImageUrl = savedPaths[0];
            }

            if (savedPaths.Count > 1)
            {
                product.HoverImageUrl = savedPaths[1];
            }
            else if (!preserveExisting || string.IsNullOrWhiteSpace(product.HoverImageUrl))
            {
                product.HoverImageUrl = product.ImageUrl;
            }

            var extraImages = savedPaths.Skip(2).ToList();
            if (extraImages.Count > 0)
            {
                product.AdditionalImageUrls = string.Join(';', extraImages);
            }
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Product_ID == id);
        }

        private async Task TrackProductViewAsync(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var visitorKey = Request.Cookies["visitor_key"];

            if (string.IsNullOrWhiteSpace(visitorKey))
            {
                visitorKey = Guid.NewGuid().ToString("N");
                Response.Cookies.Append("visitor_key", visitorKey, new CookieOptions
                {
                    HttpOnly = true,
                    IsEssential = true,
                    Expires = DateTimeOffset.UtcNow.AddMonths(6)
                });
            }

            _context.ProductViews.Add(new ProductView
            {
                ProductId = productId,
                UserId = userId,
                VisitorKey = visitorKey,
                ViewedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }
    }
}

