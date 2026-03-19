using ProductStore.Models;

namespace ProductStore.Services
{
    public interface IJwtTokenService
    {
        string CreateToken(ApplicationUser user, IList<string> roles);
    }
}

