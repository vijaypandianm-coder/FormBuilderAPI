using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using FormBuilderAPI.Models.SqlModels;

namespace FormBuilderAPI.Data
{
    /// <summary>
    /// Seeds a single Admin user into the SQL Users table (if missing).
    /// </summary>
    public class DatabaseSeeder
    {
        private readonly SqlDbContext _db;
        private readonly IConfiguration _config;

        public DatabaseSeeder(SqlDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public async Task SeedAdminAsync()
        {
            // Make sure DB exists / can connect; if not, Migrations must be applied first.
            await _db.Database.CanConnectAsync();

            // If there is already any Admin, do nothing.
            var anyAdmin = await _db.Users.AnyAsync(u => u.Role == "Admin");
            if (anyAdmin) return;

            // Read from appsettings.json (optional)
            var emailFromConfig = _config["Seed:AdminEmail"];
            var passwordFromConfig = _config["Seed:AdminPassword"];

            var adminEmail = string.IsNullOrWhiteSpace(emailFromConfig)
                ? "admin@example.com"
                : emailFromConfig.Trim();

            var adminPassword = string.IsNullOrWhiteSpace(passwordFromConfig)
                ? "Admin@123"
                : passwordFromConfig;

            // Create admin user
            var admin = new User
            {
                Username   = "admin",
                Email      = adminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                Role       = "Admin",
                IsActive   = true,
                CreatedAt  = DateTime.UtcNow
            };

            _db.Users.Add(admin);
            await _db.SaveChangesAsync();
        }
    }
}