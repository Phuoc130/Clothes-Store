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
                // ── Men ────────────────────────────────────────────────
                new()
                {
                    Name = "Áo Polo Classic Fit",
                    Description = "Áo polo nam chất liệu cotton 100% mềm mại, thoáng mát. Thiết kế cổ bẻ thanh lịch, phù hợp đi làm và dạo phố. Form regular fit thoải mái.",
                    Category = "Men",
                    Type = "Polo",
                    Price = 450000,
                    ColorHex = "#1a1a2e",
                    SizesCsv = "S,M,L,XL",
                    Availability = true,
                    Stock = 50,
                    ImageUrl = "https://images.unsplash.com/photo-1625910513413-5fc7e7a17b23?q=80&w=800&auto=format&fit=crop",
                    HoverImageUrl = "https://images.unsplash.com/photo-1586790170083-2f9ceadc732d?q=80&w=800&auto=format&fit=crop"
                },
                new()
                {
                    Name = "Áo Sơ Mi Linen Trắng",
                    Description = "Sơ mi nam chất linen cao cấp, thoáng khí tuyệt vời cho mùa hè. Phom dáng slim fit, cổ đứng hiện đại. Phù hợp phối với quần tây hoặc jeans.",
                    Category = "Men",
                    Type = "Shirt",
                    Price = 690000,
                    ColorHex = "#f5f5f0",
                    SizesCsv = "S,M,L,XL,XXL",
                    Availability = true,
                    Stock = 35,
                    ImageUrl = "https://images.unsplash.com/photo-1596755094514-f87e34085b2c?q=80&w=800&auto=format&fit=crop",
                    HoverImageUrl = "https://images.unsplash.com/photo-1603252109303-2751441dd157?q=80&w=800&auto=format&fit=crop"
                },
                new()
                {
                    Name = "Quần Kaki Slim Fit",
                    Description = "Quần kaki nam form slim fit ôm vừa vặn, chất vải cotton pha spandex co giãn tốt. Đường may tỉ mỉ, phù hợp công sở và đời thường.",
                    Category = "Men",
                    Type = "Pants",
                    Price = 590000,
                    DiscountPercent = 15,
                    ColorHex = "#c4a882",
                    SizesCsv = "29,30,31,32,33,34",
                    Availability = true,
                    Stock = 40,
                    ImageUrl = "https://images.unsplash.com/photo-1473966968600-fa801b869a1a?q=80&w=800&auto=format&fit=crop",
                    HoverImageUrl = "https://images.unsplash.com/photo-1624378439575-d8705ad7ae80?q=80&w=800&auto=format&fit=crop"
                },
                new()
                {
                    Name = "Áo Thun Oversize",
                    Description = "Áo thun nam oversize chất cotton 2 chiều dày dặn. In hình minimalist, form rộng thoải mái. Phù hợp phong cách streetwear.",
                    Category = "Men",
                    Type = "T-Shirt",
                    Price = 350000,
                    ColorHex = "#2d2d2d",
                    SizesCsv = "M,L,XL",
                    Availability = true,
                    Stock = 60,
                    ImageUrl = "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?q=80&w=800&auto=format&fit=crop",
                    HoverImageUrl = "https://images.unsplash.com/photo-1583743814966-8936f5b7be1a?q=80&w=800&auto=format&fit=crop"
                },
                new()
                {
                    Name = "Blazer Wool Pha",
                    Description = "Blazer nam chất wool pha, lót lụa mềm mại. Thiết kế 2 khuy cổ điển, vai raglan tạo dáng đẹp. Lý tưởng cho các buổi họp và sự kiện.",
                    Category = "Men",
                    Type = "Outerwear",
                    Price = 1890000,
                    DiscountPercent = 10,
                    ColorHex = "#1c1c1c",
                    SizesCsv = "M,L,XL",
                    Availability = true,
                    Stock = 12,
                    ImageUrl = "https://images.unsplash.com/photo-1507679799987-c73779587ccf?q=80&w=800&auto=format&fit=crop",
                    HoverImageUrl = "https://images.unsplash.com/photo-1593030761757-71fae45fa0e7?q=80&w=800&auto=format&fit=crop"
                },
                new()
                {
                    Name = "Quần Jeans Straight",
                    Description = "Quần jeans nam ống suông chất denim 12oz cao cấp. Wash nhẹ tạo hiệu ứng vintage. Đường may kép bền chắc, túi kiểu 5 pocket quen thuộc.",
                    Category = "Men",
                    Type = "Pants",
                    Price = 750000,
                    ColorHex = "#4a6fa5",
                    SizesCsv = "29,30,31,32,33,34",
                    Availability = true,
                    Stock = 45,
                    ImageUrl = "https://images.unsplash.com/photo-1542272604-787c3835535d?q=80&w=800&auto=format&fit=crop",
                    HoverImageUrl = "https://images.unsplash.com/photo-1604176354204-9268737828e4?q=80&w=800&auto=format&fit=crop"
                },
                new()
                {
                    Name = "Áo Hoodie Essential",
                    Description = "Hoodie unisex chất nỉ bông dày dặn, lót lông mịn bên trong. Mũ trùm có dây rút, túi kangaroo tiện dụng. Ấm áp cho ngày se lạnh.",
                    Category = "Men",
                    Type = "Hoodie",
                    Price = 520000,
                    ColorHex = "#6b705c",
                    SizesCsv = "M,L,XL,XXL",
                    Availability = true,
                    Stock = 30,
                    ImageUrl = "https://images.unsplash.com/photo-1556821840-3a63f95609a7?q=80&w=800&auto=format&fit=crop",
                    HoverImageUrl = "https://images.unsplash.com/photo-1578768079470-e638e22dc62c?q=80&w=800&auto=format&fit=crop"
                },

                // ── Women ──────────────────────────────────────────────
                new()
                {
                    Name = "Đầm Midi Hoa Nhí",
                    Description = "Đầm midi nữ họa tiết hoa nhí vintage, chất vải voan mềm rủ. Cổ V thanh lịch, eo co chun tạo form. Phù hợp dạo phố, đi chơi, chụp hình.",
                    Category = "Women",
                    Type = "Dress",
                    Price = 680000,
                    DiscountPercent = 20,
                    ColorHex = "#e8d5c4",
                    SizesCsv = "S,M,L",
                    Availability = true,
                    Stock = 25,
                    ImageUrl = "https://images.unsplash.com/photo-1572804013309-59a88b7e92f1?q=80&w=800&auto=format&fit=crop",
                    HoverImageUrl = "https://images.unsplash.com/photo-1496747611176-843222e1e57c?q=80&w=800&auto=format&fit=crop"
                },
                new()
                {
                    Name = "Áo Blouse Lụa",
                    Description = "Áo blouse nữ chất lụa cao cấp, bóng mượt và thoáng mát. Thiết kế cổ nơ thanh lịch, tay bồng nhẹ nhàng. Phối cùng chân váy hoặc quần tây.",
                    Category = "Women",
                    Type = "Blouse",
                    Price = 780000,
                    ColorHex = "#d4a574",
                    SizesCsv = "S,M,L,XL",
                    Availability = true,
                    Stock = 18,
                    ImageUrl = "https://images.unsplash.com/photo-1485462537746-965f33f7f6a7?q=80&w=800&auto=format&fit=crop",
                    HoverImageUrl = "https://images.unsplash.com/photo-1524504388940-b1c1722653e1?q=80&w=800&auto=format&fit=crop"
                },
                new()
                {
                    Name = "Chân Váy Xếp Ly",
                    Description = "Chân váy xếp ly midi chất vải dày dặn, không nhăn. Lưng thun thoải mái, phù hợp nhiều dáng người. Phong cách Hàn Quốc trẻ trung.",
                    Category = "Women",
                    Type = "Skirt",
                    Price = 420000,
                    ColorHex = "#2f3e46",
                    SizesCsv = "S,M,L",
                    Availability = true,
                    Stock = 32,
                    ImageUrl = "https://images.unsplash.com/photo-1583496661160-fb5886a0aaaa?q=80&w=800&auto=format&fit=crop",
                    HoverImageUrl = "https://images.unsplash.com/photo-1594633312681-425c7b97ccd1?q=80&w=800&auto=format&fit=crop"
                },
                new()
                {
                    Name = "Áo Khoác Trench Coat",
                    Description = "Áo khoác trench coat nữ dáng dài, chất vải kaki chống gió nhẹ. Đai lưng thắt eo tạo form. Phom oversized phù hợp layering nhiều lớp.",
                    Category = "Women",
                    Type = "Outerwear",
                    Price = 1290000,
                    DiscountPercent = 10,
                    ColorHex = "#d9cfc1",
                    SizesCsv = "S,M,L",
                    Availability = true,
                    Stock = 14,
                    ImageUrl = "https://images.unsplash.com/photo-1487222477894-8943e31ef7b2?q=80&w=800&auto=format&fit=crop",
                    HoverImageUrl = "https://images.unsplash.com/photo-1490481651871-ab68de25d43d?q=80&w=800&auto=format&fit=crop"
                },
                new()
                {
                    Name = "Set Đồ Bộ Lụa",
                    Description = "Bộ đồ ngủ / đồ mặc nhà chất lụa satin mềm mịn. Gồm áo tay ngắn và quần dài ống rộng. Thoải mái, thanh lịch khi ở nhà.",
                    Category = "Women",
                    Type = "Knitwear",
                    Price = 550000,
                    ColorHex = "#be95c4",
                    SizesCsv = "S,M,L",
                    Availability = true,
                    Stock = 20,
                    ImageUrl = "https://images.unsplash.com/photo-1515886657613-9f3515b0c78f?q=80&w=800&auto=format&fit=crop",
                    HoverImageUrl = "https://images.unsplash.com/photo-1509631179647-0177331693ae?q=80&w=800&auto=format&fit=crop"
                },
                new()
                {
                    Name = "Quần Culottes",
                    Description = "Quần ống rộng culottes chất vải lanh mát mẻ. Lưng thun, phom rộng thoải mái. Phù hợp đi làm và đi chơi, tạo cảm giác nhẹ nhàng thanh thoát.",
                    Category = "Women",
                    Type = "Pants",
                    Price = 480000,
                    ColorHex = "#e9edc9",
                    SizesCsv = "S,M,L,XL",
                    Availability = true,
                    Stock = 28,
                    ImageUrl = "https://images.unsplash.com/photo-1509631179647-0177331693ae?q=80&w=800&auto=format&fit=crop",
                    HoverImageUrl = "https://images.unsplash.com/photo-1515886657613-9f3515b0c78f?q=80&w=800&auto=format&fit=crop"
                },
                new()
                {
                    Name = "Áo Crop Top Thể Thao",
                    Description = "Áo crop top thể thao nữ chất thun cotton pha spandex co giãn 4 chiều. Viền cổ tròn, tay ngắn ôm body. Lý tưởng tập gym và yoga.",
                    Category = "Women",
                    Type = "T-Shirt",
                    Price = 280000,
                    ColorHex = "#264653",
                    SizesCsv = "S,M,L",
                    Availability = true,
                    Stock = 45,
                    ImageUrl = "https://images.unsplash.com/photo-1518459031867-a89b944bffe4?q=80&w=800&auto=format&fit=crop",
                    HoverImageUrl = "https://images.unsplash.com/photo-1552374196-1ab2a1c593e8?q=80&w=800&auto=format&fit=crop"
                },

                // ── Collection ─────────────────────────────────────────
                new()
                {
                    Name = "Áo Cardigan Len Mỏng",
                    Description = "Cardigan len mỏng unisex phong cách Hàn Quốc. Nút cài phía trước, form rộng thoải mái. Lý tưởng để layering hoặc khoác nhẹ ngoài áo thun.",
                    Category = "Collection",
                    Type = "Knitwear",
                    Price = 620000,
                    DiscountPercent = 5,
                    ColorHex = "#a68a64",
                    SizesCsv = "M,L,XL",
                    Availability = true,
                    Stock = 22,
                    ImageUrl = "https://images.unsplash.com/photo-1434389677669-e08b4cac3105?q=80&w=800&auto=format&fit=crop",
                    HoverImageUrl = "https://images.unsplash.com/photo-1490481651871-ab68de25d43d?q=80&w=800&auto=format&fit=crop"
                },
                new()
                {
                    Name = "Quần Short Đũi",
                    Description = "Short linen unisex mát mẻ, lưng thun dây rút tiện lợi. Chất vải đũi tự nhiên, thoáng khí. Phù hợp đi biển, đi chơi mùa hè.",
                    Category = "Collection",
                    Type = "Shorts",
                    Price = 320000,
                    ColorHex = "#ddb892",
                    SizesCsv = "S,M,L,XL",
                    Availability = true,
                    Stock = 38,
                    ImageUrl = "https://images.unsplash.com/photo-1591195853828-11db59a44f6b?q=80&w=800&auto=format&fit=crop",
                    HoverImageUrl = "https://images.unsplash.com/photo-1562157873-818bc0726f68?q=80&w=800&auto=format&fit=crop"
                },
                new()
                {
                    Name = "Jacket Denim Vintage",
                    Description = "Áo khoác jeans oversized wash vintage, chất denim dày dặn. Nút kim loại chắc chắn, túi ngực kiểu classic. Dễ phối với mọi outfit.",
                    Category = "Collection",
                    Type = "Outerwear",
                    Price = 890000,
                    DiscountPercent = 15,
                    ColorHex = "#6c757d",
                    SizesCsv = "M,L,XL",
                    Availability = true,
                    Stock = 16,
                    ImageUrl = "https://images.unsplash.com/photo-1551028719-00167b16eac5?q=80&w=800&auto=format&fit=crop",
                    HoverImageUrl = "https://images.unsplash.com/photo-1544022613-e87ca75a784a?q=80&w=800&auto=format&fit=crop"
                },
                new()
                {
                    Name = "Áo Sweater Cổ Tròn",
                    Description = "Sweater unisex chất nỉ bông mềm mại, cổ tròn basic. Phom regular fit, phù hợp mặc đơn hoặc layering. Màu trung tính dễ phối đồ.",
                    Category = "Collection",
                    Type = "Knitwear",
                    Price = 480000,
                    ColorHex = "#9a8c70",
                    SizesCsv = "S,M,L,XL",
                    Availability = true,
                    Stock = 30,
                    ImageUrl = "https://images.unsplash.com/photo-1576566588028-4147f3842f27?q=80&w=800&auto=format&fit=crop",
                    HoverImageUrl = "https://images.unsplash.com/photo-1556821840-3a63f95609a7?q=80&w=800&auto=format&fit=crop"
                },
                new()
                {
                    Name = "Giày Sneaker Trắng",
                    Description = "Giày sneaker trắng cổ thấp, đế cao su chống trượt. Chất liệu da tổng hợp dễ vệ sinh. Thiết kế tối giản phù hợp mọi trang phục.",
                    Category = "Collection",
                    Type = "Shoes",
                    Price = 840000,
                    ColorHex = "#f8f9fa",
                    SizesCsv = "39,40,41,42,43,44",
                    Availability = true,
                    Stock = 20,
                    ImageUrl = "https://images.unsplash.com/photo-1549298916-b41d501d3772?q=80&w=800&auto=format&fit=crop",
                    HoverImageUrl = "https://images.unsplash.com/photo-1514989940723-e8e51635b782?q=80&w=800&auto=format&fit=crop"
                },
                new()
                {
                    Name = "Túi Tote Vải Canvas",
                    Description = "Túi tote canvas dày dặn, in logo thêu tay tinh tế. Quai dài đeo vai, ngăn phụ bên trong có kéo khóa. Phong cách tối giản bền bỉ.",
                    Category = "Collection",
                    Type = "Bag",
                    Price = 380000,
                    ColorHex = "#ccd5ae",
                    SizesCsv = "Free Size",
                    Availability = true,
                    Stock = 50,
                    ImageUrl = "https://images.unsplash.com/photo-1594633312681-425c7b97ccd1?q=80&w=800&auto=format&fit=crop",
                    HoverImageUrl = "https://images.unsplash.com/photo-1543076447-215ad9ba6923?q=80&w=800&auto=format&fit=crop"
                },
                new()
                {
                    Name = "Mũ Bucket Hat",
                    Description = "Mũ bucket hat unisex chất vải cotton thoáng mát. Vành rộng che nắng tốt, có lỗ thoáng khí. Có thể gấp gọn bỏ túi tiện lợi.",
                    Category = "Collection",
                    Type = "Accessories",
                    Price = 220000,
                    ColorHex = "#b5838d",
                    SizesCsv = "Free Size",
                    Availability = true,
                    Stock = 40,
                    ImageUrl = "https://images.unsplash.com/photo-1521369909029-2afed882baee?q=80&w=800&auto=format&fit=crop",
                    HoverImageUrl = "https://images.unsplash.com/photo-1576566588028-4147f3842f27?q=80&w=800&auto=format&fit=crop"
                },
                new()
                {
                    Name = "Áo Bomber Limited Edition",
                    Description = "Áo khoác bomber phiên bản giới hạn, chất nylon cao cấp chống nước nhẹ. Lót lông cừu ấm áp, khóa kéo YKK. Sản phẩm đã sold out.",
                    Category = "Men",
                    Type = "Outerwear",
                    Price = 1590000,
                    ColorHex = "#344e41",
                    SizesCsv = "M,L,XL",
                    Availability = true,
                    Stock = 0
                },
                new()
                {
                    Name = "Vest Cưới Premium",
                    Description = "Vest cưới nam cao cấp, chất liệu wool pha silk. Thiết kế riêng theo yêu cầu. Hiện đang tạm ngừng nhận đơn.",
                    Category = "Men",
                    Type = "Suit",
                    Price = 3500000,
                    ColorHex = "#212529",
                    SizesCsv = "M,L,XL",
                    Availability = false,
                    Stock = 5
                }
            };

            context.Products.AddRange(products);
            context.SaveChanges();
        }
    }
}
