using System.ComponentModel.DataAnnotations;

namespace ProductStore.Models
{
    public class CartItem
    {
        [Key]
        public int CartItemId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int ProductId { get; set; }

        [Range(1, 99)]
        public int Quantity { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Product? Product { get; set; }
    }
}

