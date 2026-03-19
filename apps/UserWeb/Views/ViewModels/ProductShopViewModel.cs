using UserWeb.Models;

namespace UserWeb.Views.ViewModels
{
    public class ProductShopViewModel
    {
        public IEnumerable<Product> Products { get; set; } = Enumerable.Empty<Product>();
        public IReadOnlyList<string> Categories { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> Sizes { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> Colors { get; set; } = Array.Empty<string>();

        public string? Search { get; set; }
        public string? Category { get; set; }
        public string? Size { get; set; }
        public string? Color { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string Sort { get; set; } = "newest";

        public int LoadCount { get; set; }
        public int TotalCount { get; set; }
        public bool HasMore => LoadCount < TotalCount;
    }
}


