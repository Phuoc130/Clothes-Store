using System.Globalization;
using System.Net.Http.Headers;
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

        NormalizeDecimalFields(product);

        if (!ModelState.IsValid)
        {
            return View(product);
        }

        var client = _httpClientFactory.CreateClient("BackendApi");
        using var content = BuildProductFormData(product, imageFiles);
        using var response = await client.PostAsync("api/admin/products", content);
        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, $"Create failed: {message}");
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

        NormalizeDecimalFields(product);

        if (!ModelState.IsValid)
        {
            return View(product);
        }

        var client = _httpClientFactory.CreateClient("BackendApi");
        using var content = BuildProductFormData(product, imageFiles);
        using var response = await client.PutAsync($"api/admin/products/{id}", content);
        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, $"Update failed: {message}");
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

    private void NormalizeDecimalFields(Product product)
    {
        ParseAndAssignDecimal(nameof(Product.Price), value => product.Price = value, required: true);
        ParseAndAssignDecimal(nameof(Product.DiscountPercent), value => product.DiscountPercent = value, required: false);

        void ParseAndAssignDecimal(string fieldName, Action<decimal> assign, bool required)
        {
            var raw = Request.Form[fieldName].ToString().Trim();

            if (string.IsNullOrWhiteSpace(raw))
            {
                if (required)
                {
                    ModelState.AddModelError(fieldName, "Price is required.");
                }
                return;
            }

            if (TryParseFlexibleDecimal(raw, out var parsed))
            {
                assign(parsed);
                ModelState.Remove(fieldName);
                return;
            }

            ModelState.AddModelError(fieldName, "Invalid number format.");
        }
    }

    private static bool TryParseFlexibleDecimal(string input, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var normalized = input.Trim().Replace(" ", string.Empty);
        var styles = NumberStyles.Number;

        // Prefer Vietnamese-style parsing first to avoid reading "139900,00" as 13,990,000.
        if (decimal.TryParse(normalized, styles, CultureInfo.GetCultureInfo("vi-VN"), out value))
        {
            return true;
        }

        if (decimal.TryParse(normalized, styles, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        // Fallback: infer decimal separator from the last punctuation mark.
        var lastDot = normalized.LastIndexOf('.');
        var lastComma = normalized.LastIndexOf(',');
        if (lastDot >= 0 || lastComma >= 0)
        {
            var decimalSeparator = lastComma > lastDot ? ',' : '.';
            var thousandsSeparator = decimalSeparator == ',' ? '.' : ',';
            normalized = normalized.Replace(thousandsSeparator.ToString(), string.Empty);
            normalized = normalized.Replace(decimalSeparator, '.');
            return decimal.TryParse(normalized, styles, CultureInfo.InvariantCulture, out value);
        }

        return false;
    }

    private static MultipartFormDataContent BuildProductFormData(Product product, List<IFormFile>? imageFiles)
    {
        var content = new MultipartFormDataContent();

        Add("Product_ID", product.Product_ID.ToString(CultureInfo.InvariantCulture));
        Add("Name", product.Name);
        Add("Description", product.Description);
        Add("Price", product.Price.ToString(CultureInfo.InvariantCulture));
        Add("DiscountPercent", product.DiscountPercent?.ToString(CultureInfo.InvariantCulture));
        Add("ImageUrl", product.ImageUrl);
        Add("HoverImageUrl", product.HoverImageUrl);
        Add("AdditionalImageUrls", product.AdditionalImageUrls);
        Add("Category", product.Category);
        Add("ColorHex", product.ColorHex);
        Add("SizesCsv", product.SizesCsv);
        Add("Type", product.Type);
        Add("Availability", product.Availability.ToString());
        Add("Stock", product.Stock.ToString(CultureInfo.InvariantCulture));
        Add("Material_ID", product.Material_ID?.ToString(CultureInfo.InvariantCulture));
        Add("Subcontractor_ID", product.Subcontractor_ID?.ToString(CultureInfo.InvariantCulture));

        if (imageFiles != null)
        {
            foreach (var file in imageFiles.Where(f => f != null && f.Length > 0))
            {
                var streamContent = new StreamContent(file.OpenReadStream());
                if (!string.IsNullOrWhiteSpace(file.ContentType))
                {
                    streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
                }
                content.Add(streamContent, "imageFiles", file.FileName);
            }
        }

        return content;

        void Add(string key, string? val)
        {
            if (string.IsNullOrWhiteSpace(val))
            {
                return;
            }
            content.Add(new StringContent(val), key);
        }
    }
}

