using System.ComponentModel.DataAnnotations;

namespace ProductStore.Models
{
    public class Customer
    {
        [Key]
        public int Customer_ID { get; set; }
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public string? Address { get; set; }
        public int Age { get; set; }
        public string? PostalCode { get; set; }
        public string? Email { get; set; }
        public string? Gender { get; set; }

        public int? Event_ID { get; set; }
        public Event? Event { get; set; }

        public int? Invoice_ID { get; set; }
        public Invoice? Invoice { get; set; }

        public int? Order_ID { get; set; }
        public Order? Order { get; set; }
    }
}

