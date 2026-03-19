using AdminWeb.Models;

namespace AdminWeb.Views.ViewModels
{
    public class ProductAdminListViewModel
    {
        public IEnumerable<Product> Products { get; set; } = Enumerable.Empty<Product>();
        public PagingInfo PagingInfo { get; set; } = new();

        public string? Search { get; set; }
        public string? Category { get; set; }
        public string? StockFilter { get; set; }
        public IReadOnlyList<string> Categories { get; set; } = Array.Empty<string>();
    }
}


