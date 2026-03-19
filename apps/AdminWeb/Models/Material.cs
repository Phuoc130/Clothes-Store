using System.ComponentModel.DataAnnotations;

namespace AdminWeb.Models
{
    public class Material
    {
        [Key]
        public int Material_ID { get; set; }
        public string? Material_Type { get; set; }
        public bool Availability { get; set; }
        public int Stock { get; set; }

        public int Subcontractor_ID { get; set; }
        public Subcontractor? Subcontractor { get; set; }
    }
}


