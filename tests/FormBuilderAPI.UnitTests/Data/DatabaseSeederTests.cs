using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;
using FormBuilderAPI.Data;
using FormBuilderAPI.Models.SqlModels;

public class DatabaseSeederTests
{
    private static SqlDbContext NewDb()
    {
        var opts = new DbContextOptionsBuilder<SqlDbContext>()
            .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
            .Options;

        return new SqlDbContext(opts);
    }

    private static IConfiguration Config(string? email = null, string? password = null)
    {
        var dict = new Dictionary<string,string?> {
            ["Seed:AdminEmail"] = email,
            ["Seed:AdminPassword"] = password
        };
        return new ConfigurationBuilder().AddInMemoryCollection(dict!).Build();
    }

    [Fact]
    public async Task SeedAdminAsync_Adds_Admin_When_Missing()
    {
        await using var db = NewDb();
        var seeder = new DatabaseSeeder(db, Config("seeded@example.com", "Admin@123"));

        await seeder.SeedAdminAsync();

        db.Users.Count().Should().Be(1);
        var admin = db.Users.Single();
        admin.Email.Should().Be("seeded@example.com");
        admin.Role.Should().Be("Admin");
        admin.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task SeedAdminAsync_Is_Idempotent()
    {
        await using var db = NewDb();
        var seeder = new DatabaseSeeder(db, Config());

        await seeder.SeedAdminAsync();
        await seeder.SeedAdminAsync();

        db.Users.Count().Should().Be(1);
    }
}