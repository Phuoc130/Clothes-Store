using System.ComponentModel.DataAnnotations;

namespace ProductStore.Models
{
    public class ProductView
    {
        [Key]
        public int ProductViewId { get; set; }

        [Required]
        public int ProductId { get; set; }

        public string? UserId { get; set; }

        [StringLength(64)]
        public string? VisitorKey { get; set; }

        public DateTime ViewedAt { get; set; } = DateTime.UtcNow;

        public Product? Product { get; set; }
    }
}

