using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace UserWeb.Models
{
    public class Product
    {
        private const string BackendBaseUrl = "http://localhost:5181";
        private string? _imageUrl;
        private string? _hoverImageUrl;

        [Key]
        public int Product_ID { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(80)]
        public string Name { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Description { get; set; }

        public decimal Price { get; set; }

        public decimal? DiscountPercent { get; set; }

        [StringLength(300)]
        public string? ImageUrl
        {
            get => NormalizeImageUrl(_imageUrl);
            set => _imageUrl = value;
        }

        [StringLength(300)]
        public string? HoverImageUrl
        {
            get => NormalizeImageUrl(_hoverImageUrl);
            set => _hoverImageUrl = value;
        }

        [StringLength(3000)]
        public string? AdditionalImageUrls { get; set; }

        [StringLength(30)]
        public string? Category { get; set; }

        [StringLength(20)]
        public string? ColorHex { get; set; }

        [StringLength(120)]
        public string? SizesCsv { get; set; }

        public string? Type { get; set; }
        public bool Availability { get; set; }
        public int Stock { get; set; }

        public int? Material_ID { get; set; }
        public Material? Material { get; set; }

        public int? Subcontractor_ID { get; set; }
        public Subcontractor? Subcontractor { get; set; }

        public decimal FinalPrice
        {
            get
            {
                if (!DiscountPercent.HasValue || DiscountPercent <= 0)
                {
                    return Price;
                }

                var discountFactor = 1 - (DiscountPercent.Value / 100m);
                return decimal.Round(Price * discountFactor, 0, MidpointRounding.AwayFromZero);
            }
        }

        public IReadOnlyList<string> GetImageGallery()
        {
            var images = new List<string>();

            if (!string.IsNullOrWhiteSpace(ImageUrl))
            {
                images.Add(ImageUrl);
            }

            if (!string.IsNullOrWhiteSpace(HoverImageUrl) &&
                !images.Contains(HoverImageUrl, StringComparer.OrdinalIgnoreCase))
            {
                images.Add(HoverImageUrl);
            }

            if (!string.IsNullOrWhiteSpace(AdditionalImageUrls))
            {
                var extra = AdditionalImageUrls
                    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(NormalizeImageUrl)
                    .Where(url => !string.IsNullOrWhiteSpace(url))
                    .Where(url => !images.Contains(url, StringComparer.OrdinalIgnoreCase));

                images.AddRange(extra!);
            }

            return images;
        }

        public IReadOnlyList<string> GetSizes()
        {
            if (string.IsNullOrWhiteSpace(SizesCsv))
            {
                return Array.Empty<string>();
            }

            return SizesCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public string PriceDisplay => FinalPrice.ToString("N0", CultureInfo.InvariantCulture) + " VND";

        private static string? NormalizeImageUrl(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return raw;
            }

            if (Uri.TryCreate(raw, UriKind.Absolute, out _))
            {
                return raw;
            }

            if (raw.StartsWith('/'))
            {
                return BackendBaseUrl + raw;
            }

            if (raw.StartsWith("api/", StringComparison.OrdinalIgnoreCase))
            {
                return BackendBaseUrl + "/" + raw;
            }

            return raw;
        }
    }
}


