using System.ComponentModel.DataAnnotations;

namespace ProductStore.Models
{
    public class ShopOrder
    {
        [Key]
        public int ShopOrderId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Range(0, 100000000)]
        public decimal SubTotalAmount { get; set; }

        [Range(0, 100000000)]
        public decimal DiscountAmount { get; set; }

        [Range(0, 100000000)]
        public decimal ShippingFee { get; set; }

        [Range(0, 100000000)]
        public decimal TotalAmount { get; set; }

        [StringLength(128)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(128)]
        public string ProvinceCity { get; set; } = string.Empty;

        [StringLength(128)]
        public string District { get; set; } = string.Empty;

        [StringLength(300)]
        public string AddressLine { get; set; } = string.Empty;

        [StringLength(500)]
        public string? OrderNote { get; set; }

        [StringLength(20)]
        public string PaymentMethod { get; set; } = "cod";

        [StringLength(30)]
        public string? CouponCode { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<ShopOrderItem> Items { get; set; } = new();
    }
}

