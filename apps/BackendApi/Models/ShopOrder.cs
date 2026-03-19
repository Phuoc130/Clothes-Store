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
        public decimal TotalAmount { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<ShopOrderItem> Items { get; set; } = new();
    }
}

