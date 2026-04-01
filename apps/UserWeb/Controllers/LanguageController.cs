using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace UserWeb.Controllers;

public class LanguageController : Controller
{
    private static readonly HashSet<string> SupportedCultures = new(StringComparer.OrdinalIgnoreCase)
    {
        "vi-VN",
        "en-US"
    };

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetLanguage(string? culture, string? returnUrl)
    {
        var selectedCulture = SupportedCultures.Contains(culture ?? string.Empty)
            ? culture!
            : "vi-VN";

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(selectedCulture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true
            });

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }
}
