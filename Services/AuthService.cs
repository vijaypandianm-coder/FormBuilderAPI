using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using FormBuilderAPI.Data;
using FormBuilderAPI.Models.SqlModels;

namespace FormBuilderAPI.Services
{
    public class AuthService
    {
        private readonly SqlDbContext _db;
        private readonly IConfiguration _config;

        public AuthService(SqlDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        // ✅ Register new user
        public async Task<User> RegisterAsync(string username, string email, string password, string role = "Learner")
        {
            var exists = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (exists != null)
                throw new Exception("User already exists");

            var newUser = new User
            {
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(newUser);
            await _db.SaveChangesAsync();
            return newUser;
        }

        // ✅ Login
        public async Task<string?> LoginAsync(string email, string password)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || !user.IsActive)
                return null;

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return null;

            return GenerateJwtToken(user);
        }

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(3),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}