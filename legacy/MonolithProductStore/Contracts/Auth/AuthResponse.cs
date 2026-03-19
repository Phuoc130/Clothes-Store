namespace ProductStore.Contracts.Auth
{
    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
        public string Email { get; set; } = string.Empty;
    }
}

