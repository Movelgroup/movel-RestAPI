    

namespace apiEndpointNameSpace.Models.Auth
{
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string? ErrorMessage { get; set; }
        public string? UserId { get; set; }
        public string? Email { get; set; }
        public List<string> AllowedChargers { get; set; } = new();
    }
}