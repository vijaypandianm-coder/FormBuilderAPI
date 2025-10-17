using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

using FormBuilderAPI.Data;
using FormBuilderAPI.Services;

namespace FormBuilderAPI.UnitTests.Services
{
    public class AuthServiceTests
    {
        private static SqlDbContext NewDb(string name)
        {
            var opts = new DbContextOptionsBuilder<SqlDbContext>()
                .UseInMemoryDatabase(name)
                .Options;
            return new SqlDbContext(opts);
        }

        private static IConfiguration NewConfig() =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"]   = "test-issuer",
                    ["Jwt:Audience"] = "test-audience",
                    ["Jwt:Key"]      = "super-secret-test-key-1234567890"
                })
                .Build();

        [Fact]
        public async Task Register_then_Login_success_returns_token()
        {
            await using var db = NewDb(nameof(Register_then_Login_success_returns_token));
            var auth = new AuthService(db, NewConfig());

            var user = await auth.RegisterAsync("alice", "alice@example.com", "P@ssw0rd!", "Learner");
            user.Username.Should().Be("alice");

            var token = await auth.LoginAsync("alice@example.com", "P@ssw0rd!");
            token.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task Login_admin_bypass_returns_token()
        {
            await using var db = NewDb(nameof(Login_admin_bypass_returns_token));
            var auth = new AuthService(db, NewConfig());

            var token = await auth.LoginAsync("admin@example.com", "Admin@123");
            token.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task Login_with_wrong_password_returns_null()
        {
            await using var db = NewDb(nameof(Login_with_wrong_password_returns_null));
            var auth = new AuthService(db, NewConfig());

            await auth.RegisterAsync("bob", "bob@example.com", "Secret123!", "Learner");

            var token = await auth.LoginAsync("bob@example.com", "wrong");
            token.Should().BeNull();
        }

        [Fact]
        public async Task Login_with_unknown_email_returns_null()
        {
            await using var db = NewDb(nameof(Login_with_unknown_email_returns_null));
            var auth = new AuthService(db, NewConfig());

            var token = await auth.LoginAsync("nobody@example.com", "whatever");
            token.Should().BeNull();
        }

        [Fact]
        public async Task Register_duplicate_email_throws()
        {
            await using var db = NewDb(nameof(Register_duplicate_email_throws));
            var auth = new AuthService(db, NewConfig());

            await auth.RegisterAsync("eve", "eve@example.com", "Strong#1", "Learner");

            var act = async () => await auth.RegisterAsync("eve2", "eve@example.com", "Strong#2", "Learner");
            await act.Should().ThrowAsync<System.Exception>()
                .WithMessage("*already exists*");
        }

        [Fact]
        public async Task Register_non_learner_throws()
        {
            await using var db = NewDb(nameof(Register_non_learner_throws));
            var auth = new AuthService(db, NewConfig());

            var act = async () => await auth.RegisterAsync("john", "john@example.com", "pwd", "Admin");
            await act.Should().ThrowAsync<System.Exception>()
                .WithMessage("*Only Learner accounts*");
        }
    }
}
