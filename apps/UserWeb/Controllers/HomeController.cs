using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using UserWeb.Models;

namespace UserWeb.Controllers;

public class HomeController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public HomeController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index()
    {
        var client = _httpClientFactory.CreateClient("BackendApi");
        using var response = await client.GetAsync("api/public/products/featured?take=8");

        if (!response.IsSuccessStatusCode)
        {
            return View(Enumerable.Empty<Product>());
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        var products = await JsonSerializer.DeserializeAsync<List<Product>>(stream, JsonOptions)
            ?? new List<Product>();

        return View(products);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

