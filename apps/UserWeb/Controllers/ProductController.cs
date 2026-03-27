using System.Net.Http.Headers;
using System.Text;
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

    [HttpGet]
    public async Task<IActionResult> SearchAjax(string keyword, int limit = 48, int offset = 0)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return BadRequest(new { error = "keyword is required" });
        }

        var client = _httpClientFactory.CreateClient("BackendApi");
        var query = new Dictionary<string, string?>
        {
            ["keyword"] = keyword.Trim(),
            ["limit"] = limit.ToString(),
            ["offset"] = offset.ToString()
        };

        var queryString = string.Join("&", query
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}"));

        using var response = await client.GetAsync($"api/public/products/search?{queryString}");
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode, body);
        }

        return Content(body, "application/json");
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

    [HttpPost]
    public async Task<IActionResult> AddToCart(int productId, int qty = 1, string? size = null)
    {
        var token = GetAuthTokenFromCookie();
        if (string.IsNullOrWhiteSpace(token))
        {
            return Json(new { success = false, message = "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.", requireLogin = true });
        }

        try
        {
            var client = CreateAuthedClient(token);
            var payload = JsonSerializer.Serialize(new { ProductId = productId, Quantity = Math.Max(qty, 1), Size = size });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            using var response = await client.PostAsync("api/cart", content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    ClearAuthCookies();
                    return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.", requireLogin = true });
                }
                var error = TryParseMessage(body);
                return Json(new { success = false, message = error ?? "Không thể thêm vào giỏ hàng" });
            }

            var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body, JsonOptions);
            var msg = result != null && result.TryGetValue("message", out var m) ? m.GetString() : "Thêm vào giỏ hàng thành công";
            var count = result != null && result.TryGetValue("count", out var c) ? c.GetInt32() : 0;

            return Json(new { success = true, message = msg, count });
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[AddToCart] Exception: {ex.Message}");
            return Json(new { success = false, message = "Lỗi kết nối đến server. Vui lòng thử lại." });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetCartCount()
    {
        var token = GetAuthTokenFromCookie();
        if (string.IsNullOrWhiteSpace(token))
        {
            return Json(new { count = 0 });
        }

        var client = CreateAuthedClient(token);
        using var response = await client.GetAsync("api/cart/count");

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                ClearAuthCookies();
            }
            return Json(new { count = 0 });
        }

        var body = await response.Content.ReadAsStringAsync();
        return Content(body, "application/json");
    }

    [HttpGet]
    public async Task<IActionResult> GetCartItems()
    {
        var token = GetAuthTokenFromCookie();
        if (string.IsNullOrWhiteSpace(token))
        {
            return Json(new { success = false, requireLogin = true, items = Array.Empty<object>() });
        }

        var client = CreateAuthedClient(token);
        using var response = await client.GetAsync("api/cart");

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                ClearAuthCookies();
                return Json(new { success = false, requireLogin = true, items = Array.Empty<object>() });
            }
            return Json(new { success = false, items = Array.Empty<object>() });
        }

        var body = await response.Content.ReadAsStringAsync();
        return Content(body, "application/json");
    }

    [HttpPost]
    public async Task<IActionResult> RemoveFromCart(int productId)
    {
        var token = GetAuthTokenFromCookie();
        if (string.IsNullOrWhiteSpace(token))
        {
            return Json(new { success = false, requireLogin = true });
        }

        var client = CreateAuthedClient(token);
        using var response = await client.DeleteAsync($"api/cart/{productId}");
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                ClearAuthCookies();
                return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn", requireLogin = true });
            }
            return Json(new { success = false, message = "Không thể xóa sản phẩm" });
        }

        var result = JsonSerializer.Deserialize<JsonElement>(body);
        var count = result.TryGetProperty("count", out var c) ? c.GetInt32() : 0;

        return Json(new { success = true, message = "Đã xóa khỏi giỏ hàng", count });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateCartQuantity(int productId, int quantity)
    {
        var token = GetAuthTokenFromCookie();
        if (string.IsNullOrWhiteSpace(token))
        {
            return Json(new { success = false, requireLogin = true });
        }

        var client = CreateAuthedClient(token);
        var payload = JsonSerializer.Serialize(new { quantity });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        using var response = await client.PutAsync($"api/cart/{productId}", content);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                ClearAuthCookies();
                return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn", requireLogin = true });
            }
            return Json(new { success = false, message = "Không thể cập nhật" });
        }

        var result = JsonSerializer.Deserialize<JsonElement>(body);
        var count = result.TryGetProperty("count", out var c) ? c.GetInt32() : 0;

        return Json(new { success = true, message = "Cập nhật thành công", count });
    }

    public IActionResult Cart()
    {
        return View();
    }

    // ── Wishlist Proxy Actions ──────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> AddToWishlist(int productId)
    {
        var token = GetAuthTokenFromCookie();
        if (string.IsNullOrWhiteSpace(token))
        {
            return Json(new { success = false, message = "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.", requireLogin = true });
        }

        try
        {
            var client = CreateAuthedClient(token);
            var payload = JsonSerializer.Serialize(new { ProductId = productId, Quantity = 1 });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            using var response = await client.PostAsync("api/wishlist", content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    ClearAuthCookies();
                    return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.", requireLogin = true });
                }
                var error = TryParseMessage(body);
                return Json(new { success = false, message = error ?? "Không thể thêm vào yêu thích" });
            }

            var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body, JsonOptions);
            var msg = result != null && result.TryGetValue("message", out var m) ? m.GetString() : "Đã thêm vào yêu thích";
            var count = result != null && result.TryGetValue("count", out var c) ? c.GetInt32() : 0;
            var alreadyExisted = result != null && result.TryGetValue("alreadyExisted", out var a) && a.ValueKind == JsonValueKind.True;

            return Json(new { success = true, message = msg, count, alreadyExisted });
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[AddToWishlist] Exception: {ex.Message}");
            return Json(new { success = false, message = "Lỗi kết nối đến server. Vui lòng thử lại." });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetWishlistCount()
    {
        var token = GetAuthTokenFromCookie();
        if (string.IsNullOrWhiteSpace(token))
        {
            return Json(new { count = 0 });
        }

        var client = CreateAuthedClient(token);
        using var response = await client.GetAsync("api/wishlist/count");

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                ClearAuthCookies();
            }
            return Json(new { count = 0 });
        }

        var body = await response.Content.ReadAsStringAsync();
        return Content(body, "application/json");
    }

    [HttpGet]
    public async Task<IActionResult> GetWishlistItems()
    {
        var token = GetAuthTokenFromCookie();
        if (string.IsNullOrWhiteSpace(token))
        {
            return Json(new { success = false, requireLogin = true, items = Array.Empty<object>() });
        }

        var client = CreateAuthedClient(token);
        using var response = await client.GetAsync("api/wishlist");

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                ClearAuthCookies();
                return Json(new { success = false, requireLogin = true, items = Array.Empty<object>() });
            }
            return Json(new { success = false, items = Array.Empty<object>() });
        }

        var body = await response.Content.ReadAsStringAsync();
        return Content(body, "application/json");
    }

    [HttpPost]
    public async Task<IActionResult> RemoveFromWishlist(int productId)
    {
        var token = GetAuthTokenFromCookie();
        if (string.IsNullOrWhiteSpace(token))
        {
            return Json(new { success = false, requireLogin = true });
        }

        var client = CreateAuthedClient(token);
        using var response = await client.DeleteAsync($"api/wishlist/{productId}");
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                ClearAuthCookies();
                return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn", requireLogin = true });
            }
            return Json(new { success = false, message = "Không thể xóa sản phẩm" });
        }

        var result = JsonSerializer.Deserialize<JsonElement>(body);
        var count = result.TryGetProperty("count", out var c) ? c.GetInt32() : 0;

        return Json(new { success = true, message = "Đã xóa khỏi yêu thích", count });
    }

    public IActionResult Wishlist()
    {
        return View();
    }

    // ── Helpers ──────────────────────────────────────────────────

    private HttpClient CreateAuthedClient(string token)
    {
        var client = _httpClientFactory.CreateClient("BackendApi");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());
        return client;
    }

    private static string? TryParseMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body, options);
            if (result != null && result.TryGetValue("message", out var m))
            {
                return m.GetString();
            }
        }
        catch
        {
            // ignored
        }
        return null;
    }

    private string? GetAuthTokenFromCookie()
    {
        var raw = Request.Cookies["auth_token"];
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        raw = raw.Trim();
        const string bearerPrefix = "Bearer ";
        if (raw.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return raw[bearerPrefix.Length..].Trim();
        }

        return raw;
    }

    private void ClearAuthCookies()
    {
        Response.Cookies.Delete("auth_token");
        Response.Cookies.Delete("auth_email");
        Response.Cookies.Delete("auth_role");
    }
}
