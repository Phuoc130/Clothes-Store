using System.ComponentModel.DataAnnotations;

namespace ProductStore.Models
{
    public class ShopOrderItem
    {
        [Key]
        public int ShopOrderItemId { get; set; }

        [Required]
        public int ShopOrderId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Range(1, 99)]
        public int Quantity { get; set; }

        [Range(0, 100000000)]
        public decimal UnitPrice { get; set; }

        public ShopOrder? Order { get; set; }
        public Product? Product { get; set; }
    }
}

