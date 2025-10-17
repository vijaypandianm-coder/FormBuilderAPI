using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Xunit;

// SUT namespaces
using FormBuilderAPI.Data;
using FormBuilderAPI.Models.SqlModels;
using FormBuilderAPI.Models.MongoModels;

namespace FormBuilderAPI.UnitTests.Data
{
    public class DatabaseSeederTests
    {
        private static SqlDbContext NewInMemoryDb(string dbName)
        {
            var opts = new DbContextOptionsBuilder<SqlDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new SqlDbContext(opts);
        }

        private static IConfiguration NewConfig(IDictionary<string, string?>? values = null)
        {
            var builder = new ConfigurationBuilder();
            if (values is not null) builder.AddInMemoryCollection(values);
            return builder.Build();
        }

        [Fact]
        public async Task SeedAdminAsync_adds_admin_when_missing()
        {
            // Arrange
            using var db = NewInMemoryDb(nameof(SeedAdminAsync_adds_admin_when_missing));
            var config = NewConfig(new Dictionary<string, string?>
            {
                ["Seed:AdminEmail"] = "seeded.admin@example.com",
                ["Seed:AdminPassword"] = "Secret#123"
            });
            var seeder = new DatabaseSeeder(db, config);

            // Act
            await seeder.SeedAdminAsync();

            // Assert
            var admins = await db.Users.CountAsync(u => u.Role == "Admin");
            admins.Should().Be(1, "seeder should add exactly one Admin when none exist");

            var admin = await db.Users.SingleAsync(u => u.Role == "Admin");
            admin.Email.Should().Be("seeded.admin@example.com");
            admin.Username.Should().Be("admin");
            admin.IsActive.Should().BeTrue();
            admin.PasswordHash.Should().NotBeNullOrWhiteSpace("hash should be stored");
        }

        [Fact]
        public async Task SeedAdminAsync_does_nothing_when_admin_exists()
        {
            // Arrange
            using var db = NewInMemoryDb(nameof(SeedAdminAsync_does_nothing_when_admin_exists));
            db.Users.Add(new User
            {
                Username = "existing-admin",
                Email = "already@there.test",
                PasswordHash = "irrelevant",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var config = NewConfig(); // no overrides -> defaults would be used, but shouldn't matter
            var seeder = new DatabaseSeeder(db, config);

            // Act
            await seeder.SeedAdminAsync();

            // Assert
            var admins = await db.Users.Where(u => u.Role == "Admin").ToListAsync();
            admins.Should().HaveCount(1, "seeder should early-exit when an Admin already exists");
            admins[0].Email.Should().Be("already@there.test");
        }
    }

    public class MongoDbContextTests
    {
        [Fact]
        public void Exposes_expected_collections_without_connecting()
        {
            // Arrange: constructing IMongoDatabase does not connect to server
            var settings = Options.Create(new MongoDbSettings
            {
                ConnectionString = "mongodb://localhost:27017",
                DatabaseName = "UnitTestDb"
            });

            // Act
            var ctx = new MongoDbContext(settings);

            // Assert: collection names are as expected
            ctx.Forms.CollectionNamespace.CollectionName.Should().Be("forms");
            ctx.Workflows.CollectionNamespace.CollectionName.Should().Be("workflows");
            ctx.Counters.CollectionNamespace.CollectionName.Should().Be("counters");
        }
    }

    public class SqlDbContextFactoryTests
    {
        [Fact]
        public void CreateDbContext_returns_context_instance()
        {
            // Arrange
            var factory = new SqlDbContextFactory();

            // Act
            var ctx = factory.CreateDbContext(Array.Empty<string>());

            // Assert (no DB connection attempted here)
            ctx.Should().NotBeNull();
            ctx.Should().BeOfType<SqlDbContext>();
            ctx.Database.ProviderName.Should().NotBeNullOrWhiteSpace();
        }
    }
}