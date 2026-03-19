using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using AdminWeb.Models;
using AdminWeb.Views.ViewModels;

namespace AdminWeb.Controllers;

public class ProductController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ProductController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Admin(int productPage = 1, string? search = null, string? category = null, string? stockFilter = null)
    {
        var client = _httpClientFactory.CreateClient("BackendApi");

        var query = new Dictionary<string, string?>
        {
            ["productPage"] = productPage.ToString(),
            ["search"] = search,
            ["category"] = category,
            ["stockFilter"] = stockFilter
        };

        var queryString = string.Join("&", query
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}"));

        using var response = await client.GetAsync($"api/admin/products?{queryString}");
        if (!response.IsSuccessStatusCode)
        {
            return View(new ProductAdminListViewModel());
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        var model = await JsonSerializer.DeserializeAsync<ProductAdminListViewModel>(stream, JsonOptions)
            ?? new ProductAdminListViewModel();

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

        if (!ModelState.IsValid)
        {
            return View(product);
        }

        var client = _httpClientFactory.CreateClient("BackendApi");
        using var response = await client.PostAsJsonAsync("api/admin/products", product);
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty, "Create failed. Please verify backend is running.");
            return View(product);
        }

        return RedirectToAction(nameof(Admin));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var client = _httpClientFactory.CreateClient("BackendApi");
        using var response = await client.GetAsync($"api/admin/products/{id.Value}");
        if (!response.IsSuccessStatusCode)
        {
            return NotFound();
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        var product = await JsonSerializer.DeserializeAsync<Product>(stream, JsonOptions);
        return product == null ? NotFound() : View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Product product, List<IFormFile>? imageFiles, string[]? selectedSizes)
    {
        if (id != product.Product_ID)
        {
            return NotFound();
        }

        product.SizesCsv = selectedSizes == null
            ? product.SizesCsv
            : string.Join(',', selectedSizes.Where(s => !string.IsNullOrWhiteSpace(s)));

        if (!ModelState.IsValid)
        {
            return View(product);
        }

        var client = _httpClientFactory.CreateClient("BackendApi");
        using var response = await client.PutAsJsonAsync($"api/admin/products/{id}", product);
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty, "Update failed. Please verify backend is running.");
            return View(product);
        }

        return RedirectToAction(nameof(Admin));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var client = _httpClientFactory.CreateClient("BackendApi");
        using var response = await client.GetAsync($"api/admin/products/{id.Value}");
        if (!response.IsSuccessStatusCode)
        {
            return NotFound();
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        var product = await JsonSerializer.DeserializeAsync<Product>(stream, JsonOptions);
        return product == null ? NotFound() : View(product);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var client = _httpClientFactory.CreateClient("BackendApi");
        await client.DeleteAsync($"api/admin/products/{id}");
        return RedirectToAction(nameof(Admin));
    }
}

