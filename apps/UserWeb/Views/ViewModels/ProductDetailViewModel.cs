using UserWeb.Models;

namespace UserWeb.Views.ViewModels
{
    public class ProductDetailViewModel
    {
        public Product Product { get; set; } = new();
        public IReadOnlyList<string> GalleryImages { get; set; } = Array.Empty<string>();
        public IEnumerable<Product> RelatedProducts { get; set; } = Enumerable.Empty<Product>();
    }
}


