using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace UserWeb.Controllers;

public class AccountController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AccountController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError(string.Empty, "Email and password are required.");
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        var client = _httpClientFactory.CreateClient("BackendApi");
        using var response = await client.PostAsJsonAsync("api/auth/login", new { email, password });

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty, "Invalid credentials.");
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        var payload = await response.Content.ReadFromJsonAsync<     AuthPayload>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (payload == null || string.IsNullOrWhiteSpace(payload.Token))
        {
            ModelState.AddModelError(string.Empty, "Login failed. Please try again.");
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            Path = "/"
        };

        Response.Cookies.Append("auth_token", payload.Token, cookieOptions);
        Response.Cookies.Append("auth_email", payload.Email ?? email, cookieOptions);
        Response.Cookies.Append("auth_role", payload.Role ?? "User", cookieOptions);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string email, string password, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError(string.Empty, "Email and password are required.");
            return View();
        }

        if (password != confirmPassword)
        {
            ModelState.AddModelError(string.Empty, "Confirm password does not match.");
            return View();
        }

        var client = _httpClientFactory.CreateClient("BackendApi");
        using var response = await client.PostAsJsonAsync("api/auth/register", new { email, password });

        if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync();
            System.Console.WriteLine($"[Register] Backend returned {response.StatusCode}: {errorText}");
            ModelState.AddModelError(string.Empty, string.IsNullOrWhiteSpace(errorText) ? "Register failed." : errorText);
            return View();
        }

        var payload = await response.Content.ReadFromJsonAsync<AuthPayload>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        System.Console.WriteLine($"[Register] Payload: Token={(payload?.Token?.Substring(0, 20) ?? "NULL")}..., Email={payload?.Email}");

        if (payload == null || string.IsNullOrWhiteSpace(payload.Token))
        {
            System.Console.WriteLine("[Register] Token is null/empty!");
            ModelState.AddModelError(string.Empty, "Register succeeded but failed to retrieve token. Please login manually.");
            return View();
        }

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            Path = "/"
        };

        Response.Cookies.Append("auth_token", payload.Token, cookieOptions);
        System.Console.WriteLine("[Register] Cookie 'auth_token' set!");
        Response.Cookies.Append("auth_email", payload.Email ?? email, cookieOptions);
        Response.Cookies.Append("auth_role", payload.Role ?? "User", cookieOptions);

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("auth_token");
        Response.Cookies.Delete("auth_email");
        Response.Cookies.Delete("auth_role");
        return RedirectToAction("Index", "Home");
    }

    public class AuthPayload
    {
        public string Token { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
        public string Email { get; set; } = string.Empty;
    }
}

