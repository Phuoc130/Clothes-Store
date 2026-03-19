using System.ComponentModel.DataAnnotations;

namespace ProductStore.Models
{
    public class Event
    {
        [Key]
        public int Event_ID { get; set; }
        public string? Location { get; set; }
        public DateTime Date { get; set; }
        public int Address_ID { get; set; } // Assuming this relates to a separate Address table if needed, otherwise can be string
    }
}
