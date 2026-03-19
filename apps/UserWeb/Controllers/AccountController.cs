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

        var payload = await response.Content.ReadFromJsonAsync<AuthPayload>(new JsonSerializerOptions
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
            Secure = true,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
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
            ModelState.AddModelError(string.Empty, string.IsNullOrWhiteSpace(errorText) ? "Register failed." : errorText);
            return View();
        }

        TempData["Success"] = "Register successful. Please login.";
        return RedirectToAction(nameof(Login));
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

    private sealed class AuthPayload
    {
        public string Token { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
        public string Email { get; set; } = string.Empty;
    }
}

