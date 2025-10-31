using FluentAssertions;
using Xunit;
using FormBuilderAPI.Services;
using FormBuilderAPI.Models.SqlModels;
using FormBuilderAPI.UnitTests.TestUtils;

public class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_Allows_Learner_And_Rejects_Duplicate()
    {
        await using var db = InMemorySql.NewDb();
        var svc = new AuthService(db, FakeConfig.Build());

        var u = await svc.RegisterAsync("vijay", "vijay@example.com", "Pass@123");
        u.Role.Should().Be("Learner");
        u.Id.Should().BeGreaterThan(0);

        var again = async () => await svc.RegisterAsync("vijay", "vijay@example.com", "X");
        await again.Should().ThrowAsync<Exception>().WithMessage("*already exists*");
    }

    [Fact]
    public async Task RegisterAsync_Only_Learner_Allowed()
    {
        await using var db = InMemorySql.NewDb();
        var svc = new AuthService(db, FakeConfig.Build());

        var act = () => svc.RegisterAsync("x","x@e.com","p","Admin");
        await act.Should().ThrowAsync<Exception>().WithMessage("*Only Learner*");
    }

    [Fact]
    public async Task Login_Admin_Bypass_Returns_Token()
    {
        await using var db = InMemorySql.NewDb();
        var svc = new AuthService(db, FakeConfig.Build());

        var token = await svc.LoginAsync("admin@example.com", "Admin@123");
        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_Learner_Success_And_Failure()
    {
        await using var db = InMemorySql.NewDb();
        var svc = new AuthService(db, FakeConfig.Build());

        var u = await svc.RegisterAsync("learner","l@e.com","P@ssw0rd");
        var ok = await svc.LoginAsync("l@e.com", "P@ssw0rd");
        ok.Should().NotBeNullOrWhiteSpace();

        var bad = await svc.LoginAsync("l@e.com", "wrong");
        bad.Should().BeNull();
    }
}