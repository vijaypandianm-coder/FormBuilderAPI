namespace FormBuilderAPI.DTOs
{
    public class RegisterRequest
    {
        public string Username { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
        public string Role { get; set; } = "Learner"; // Admin/Learner
    }

    public class LoginRequest
    {
        public string UsernameOrEmail { get; set; } = default!;
        public string Password { get; set; } = default!;
    }

    public class AuthResult
    {
        public string Token { get; set; } = default!;
        public string Username { get; set; } = default!;
        public string UserId { get; set; } = default!;
        public IEnumerable<string> Roles { get; set; } = Array.Empty<string>();
        public DateTime ExpiresAt { get; set; }
    }
}