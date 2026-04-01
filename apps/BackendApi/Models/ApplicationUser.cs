using Microsoft.AspNetCore.Identity;

namespace ProductStore.Models
{
    public class ApplicationUser : IdentityUser
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

