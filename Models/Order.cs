using System.ComponentModel.DataAnnotations;

namespace ProductStore.Models
{
    public class Order
    {
        [Key]
        public int Order_ID { get; set; }
        public string? Order_Type { get; set; }
        public string? Product_Type { get; set; }
        public string? Product_Location { get; set; }

        // Foreign Key
        public int Product_ID { get; set; }
        public Product? Product { get; set; }
    }
}
