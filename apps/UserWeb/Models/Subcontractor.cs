using System.ComponentModel.DataAnnotations;

namespace UserWeb.Models
{
    public class Subcontractor
    {
        [Key]
        public int Subcontractor_ID { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? PostalCode { get; set; }
        public string? Email { get; set; }
    }
}


