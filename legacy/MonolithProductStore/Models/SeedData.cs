namespace ProductStore.Models
{
    public static class SeedData
    {
        public static void Initialize(ProductDbContext context)
        {
            if (context.Products.Any())
            {
                return;
            }

            var products = new List<Product>
            {
                new()
                {
                    Name = "Essential Trench",
                    Description = "Structured silhouette in premium cotton blend.",
                    Category = "Women",
                    Type = "Outerwear",
                    Price = 1290000,
                    DiscountPercent = 10,
                    ColorHex = "#d9d1c3",
                    SizesCsv = "S,M,L",
                    Availability = true,
                    Stock = 24,
                    ImageUrl = "https://images.unsplash.com/photo-1487222477894-8943e31ef7b2?q=80&w=1200&auto=format&fit=crop",
                    HoverImageUrl = "https://images.unsplash.com/photo-1490481651871-ab68de25d43d?q=80&w=1200&auto=format&fit=crop"
                },
                new()
                {
                    Name = "Urban Linen Shirt",
                    Description = "Breathable linen shirt for daily sharp style.",
                    Category = "Men",
                    Type = "Shirt",
                    Price = 690000,
                    ColorHex = "#f2eee8",
                    SizesCsv = "M,L,XL",
                    Availability = true,
                    Stock = 32,
                    ImageUrl = "https://images.unsplash.com/photo-1516826957135-700dedea698c?q=80&w=1200&auto=format&fit=crop",
                    HoverImageUrl = "https://images.unsplash.com/photo-1441986300917-64674bd600d8?q=80&w=1200&auto=format&fit=crop"
                },
                new()
                {
                    Name = "Soft Knit Set",
                    Description = "Minimal knitwear set with neutral palette.",
                    Category = "Women",
                    Type = "Knitwear",
                    Price = 980000,
                    DiscountPercent = 5,
                    ColorHex = "#b7ada0",
                    SizesCsv = "S,M,L",
                    Availability = true,
                    Stock = 16,
                    ImageUrl = "https://images.unsplash.com/photo-1485462537746-965f33f7f6a7?q=80&w=1200&auto=format&fit=crop",
                    HoverImageUrl = "https://images.unsplash.com/photo-1524504388940-b1c1722653e1?q=80&w=1200&auto=format&fit=crop"
                },
                new()
                {
                    Name = "Tailored Wool Coat",
                    Description = "Classic long coat with clean structure.",
                    Category = "Men",
                    Type = "Outerwear",
                    Price = 1790000,
                    ColorHex = "#202020",
                    SizesCsv = "M,L,XL",
                    Availability = true,
                    Stock = 10,
                    ImageUrl = "https://images.unsplash.com/photo-1487222477894-8943e31ef7b2?q=80&w=1200&auto=format&fit=crop",
                    HoverImageUrl = "https://images.unsplash.com/photo-1490481651871-ab68de25d43d?q=80&w=1200&auto=format&fit=crop"
                },
                new()
                {
                    Name = "Minimal Leather Bag",
                    Description = "Everyday bag with understated details.",
                    Category = "Accessories",
                    Type = "Bag",
                    Price = 880000,
                    ColorHex = "#4b3d33",
                    SizesCsv = "M",
                    Availability = true,
                    Stock = 28,
                    ImageUrl = "https://images.unsplash.com/photo-1594633312681-425c7b97ccd1?q=80&w=1200&auto=format&fit=crop",
                    HoverImageUrl = "https://images.unsplash.com/photo-1543076447-215ad9ba6923?q=80&w=1200&auto=format&fit=crop"
                },
                new()
                {
                    Name = "Relaxed Pleat Trousers",
                    Description = "Soft drape and precise pleats for a modern fit.",
                    Category = "Men",
                    Type = "Pants",
                    Price = 750000,
                    ColorHex = "#9f9b93",
                    SizesCsv = "S,M,L,XL",
                    Availability = true,
                    Stock = 36,
                    ImageUrl = "https://images.unsplash.com/photo-1473966968600-fa801b869a1a?q=80&w=1200&auto=format&fit=crop",
                    HoverImageUrl = "https://images.unsplash.com/photo-1434389677669-e08b4cac3105?q=80&w=1200&auto=format&fit=crop"
                },
                new()
                {
                    Name = "Mono Slip Dress",
                    Description = "Clean lines with smooth satin texture.",
                    Category = "Women",
                    Type = "Dress",
                    Price = 920000,
                    ColorHex = "#d7cec2",
                    SizesCsv = "S,M,L",
                    Availability = true,
                    Stock = 20,
                    ImageUrl = "https://images.unsplash.com/photo-1464863979621-258859e62245?q=80&w=1200&auto=format&fit=crop",
                    HoverImageUrl = "https://images.unsplash.com/photo-1495385794356-15371f348c31?q=80&w=1200&auto=format&fit=crop"
                },
                new()
                {
                    Name = "Studio Sneakers",
                    Description = "Neutral-tone sneakers for smart casual looks.",
                    Category = "Accessories",
                    Type = "Shoes",
                    Price = 840000,
                    ColorHex = "#f4f4f4",
                    SizesCsv = "M,L,XL",
                    Availability = true,
                    Stock = 18,
                    ImageUrl = "https://images.unsplash.com/photo-1549298916-b41d501d3772?q=80&w=1200&auto=format&fit=crop",
                    HoverImageUrl = "https://images.unsplash.com/photo-1514989940723-e8e51635b782?q=80&w=1200&auto=format&fit=crop"
                }
            };

            context.Products.AddRange(products);
            context.SaveChanges();
        }
    }
}

