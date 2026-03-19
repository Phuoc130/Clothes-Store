using Microsoft.AspNetCore.Mvc;
using ProductStore.Models;
using ProductStore.Views.ViewModels;

public class ProductController : Controller
{
    private readonly ProductDbContext _context;
    public int PageSize = 4;

    public ProductController(ProductDbContext context)
    {
        _context = context;
    }

    public IActionResult Index(int productPage = 1)
    {
        return View(new ProductListViewModel
        {
            Products = _context.Products
                .OrderBy(p => p.Product_ID)
                .Skip((productPage - 1) * PageSize)
                .Take(PageSize),

            PagingInfo = new PagingInfo
            {
                CurrentPage = productPage,
                ItemsPerPage = PageSize,
                TotalItems = _context.Products.Count()
            }
        });
    }
}
