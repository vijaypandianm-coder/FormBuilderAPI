using Microsoft.AspNetCore.Mvc;
using FormBuilderAPI.Services;
using FormBuilderAPI.Models.MongoModels;

namespace FormBuilderAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        // ✅ Register endpoint
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            var user = await _authService.RegisterAsync(req.Username, req.Email, req.Password, req.Role);
            return Ok(new { user.Username, user.Email, user.Role });
        }

        // ✅ Login endpoint
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var token = await _authService.LoginAsync(req.Email, req.Password); // fixed parameters
            if (token == null) return Unauthorized(new { message = "Invalid credentials" });

            return Ok(new { token, role = "Bearer", expires = DateTime.UtcNow.AddHours(3) });
        }
    }

    // DTOs
    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Learner";
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}