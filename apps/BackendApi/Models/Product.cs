using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace ProductStore.Models
{
    public class Product
    {
        [Key]
        public int Product_ID { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(80)]
        public string Name { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Description { get; set; }

        [Range(0.01, 1000000)]
        public decimal Price { get; set; }

        [Range(0, 90)]
        public decimal? DiscountPercent { get; set; }

        [StringLength(300)]
        public string? ImageUrl { get; set; }

        [StringLength(300)]
        public string? HoverImageUrl { get; set; }

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
                    .Where(url => !images.Contains(url, StringComparer.OrdinalIgnoreCase));

                images.AddRange(extra);
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
    }
}

