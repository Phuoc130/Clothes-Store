namespace AdminWeb.Views.ViewModels;

public class AdminDashboardViewModel
{
    public decimal TotalRevenue { get; set; }
    public int OrderCount { get; set; }
    public int UserCount { get; set; }
    public List<TopViewedProductViewModel> TopViewedProducts { get; set; } = new();
    public List<AdminOrderViewModel> RecentOrders { get; set; } = new();
    public List<AdminUserViewModel> RecentUsers { get; set; } = new();
}

public class TopViewedProductViewModel
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Views { get; set; }
}

public class AdminOrderViewModel
{
    public int ShopOrderId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AdminUserViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public bool IsLocked { get; set; }
}

