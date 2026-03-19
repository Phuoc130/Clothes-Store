using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using UserWeb.Views.ViewModels;

namespace UserWeb.Controllers;

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

    public IActionResult Index()
    {
        return RedirectToAction(nameof(Shop));
    }

    public async Task<IActionResult> Shop(
        string? search,
        string? category,
        string? size,
        string? color,
        decimal? minPrice,
        decimal? maxPrice,
        string sort = "newest",
        int load = 8)
    {
        var client = _httpClientFactory.CreateClient("BackendApi");

        var query = new Dictionary<string, string?>
        {
            ["search"] = search,
            ["category"] = category,
            ["size"] = size,
            ["color"] = color,
            ["minPrice"] = minPrice?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["maxPrice"] = maxPrice?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["sort"] = sort,
            ["load"] = load.ToString()
        };

        var queryString = string.Join("&", query
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}"));

        using var response = await client.GetAsync($"api/public/products/shop?{queryString}");
        if (!response.IsSuccessStatusCode)
        {
            return View(new ProductShopViewModel());
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        var model = await JsonSerializer.DeserializeAsync<ProductShopViewModel>(stream, JsonOptions)
            ?? new ProductShopViewModel();

        return View(model);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var client = _httpClientFactory.CreateClient("BackendApi");
        using var response = await client.GetAsync($"api/public/products/{id.Value}");
        if (!response.IsSuccessStatusCode)
        {
            return NotFound();
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        var model = await JsonSerializer.DeserializeAsync<ProductDetailViewModel>(stream, JsonOptions)
            ?? new ProductDetailViewModel();

        return View(model);
    }
}

