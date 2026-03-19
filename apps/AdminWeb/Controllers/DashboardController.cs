using System.Text.Json;
using AdminWeb.Views.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AdminWeb.Controllers;

public class DashboardController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public DashboardController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index()
    {
        var client = _httpClientFactory.CreateClient("BackendApi");
        var model = new AdminDashboardViewModel();

        using var dashboardResponse = await client.GetAsync("api/admin/dashboard");
        if (dashboardResponse.IsSuccessStatusCode)
        {
            await using var stream = await dashboardResponse.Content.ReadAsStreamAsync();
            var dashboard = await JsonSerializer.DeserializeAsync<AdminDashboardViewModel>(stream, JsonOptions);
            if (dashboard != null)
            {
                model.TotalRevenue = dashboard.TotalRevenue;
                model.OrderCount = dashboard.OrderCount;
                model.UserCount = dashboard.UserCount;
                model.TopViewedProducts = dashboard.TopViewedProducts;
            }
        }

        using var orderResponse = await client.GetAsync("api/admin/orders");
        if (orderResponse.IsSuccessStatusCode)
        {
            await using var stream = await orderResponse.Content.ReadAsStreamAsync();
            var orders = await JsonSerializer.DeserializeAsync<List<AdminOrderViewModel>>(stream, JsonOptions);
            model.RecentOrders = orders?.Take(6).ToList() ?? new List<AdminOrderViewModel>();
        }

        using var userResponse = await client.GetAsync("api/admin/users");
        if (userResponse.IsSuccessStatusCode)
        {
            await using var stream = await userResponse.Content.ReadAsStreamAsync();
            var users = await JsonSerializer.DeserializeAsync<List<AdminUserViewModel>>(stream, JsonOptions);
            model.RecentUsers = users?.Take(6).ToList() ?? new List<AdminUserViewModel>();
        }

        return View(model);
    }
}

