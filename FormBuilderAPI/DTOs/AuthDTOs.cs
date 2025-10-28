namespace FormBuilderAPI.DTOs
{
    // Request from client -> API
    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        // Optional. Server can ignore/force "Learner"
        public string? Role { get; set; }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    // Response from API -> client
    public class AuthResultDto
    {
        public string Token { get; set; } = string.Empty;
        public string Scheme { get; set; } = "Bearer";
        public DateTime ExpiresUtc { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public long UserId { get; set; }
    }
}
