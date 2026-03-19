using System.ComponentModel.DataAnnotations;

namespace ProductStore.Models
{
    public class Product
    {
        [Key]
        public int Product_ID { get; set; }
        public string? Type { get; set; }
        public bool Availability { get; set; }
        public int Stock { get; set; }

        // Foreign Keys
        public int Material_ID { get; set; }
        public Material? Material { get; set; }

        public int Subcontractor_ID { get; set; }
        public Subcontractor? Subcontractor { get; set; }
    }
}
