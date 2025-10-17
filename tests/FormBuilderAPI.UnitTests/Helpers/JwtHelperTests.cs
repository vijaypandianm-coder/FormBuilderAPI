using System.Security.Claims;
using FluentAssertions;
using Xunit;
using FormBuilderAPI.Helpers;

namespace FormBuilderAPI.UnitTests.Helpers;

public class JwtHelperTests
{
    [Fact]
    public void Generate_And_Validate_Token_Should_Work()
    {
        var issuer = "FormBuilderAPI";
        var audience = "FormBuilderClient";
        var key = "super-secret-signing-key-1234567890";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "42"),
            new Claim(ClaimTypes.Email, "user@example.com"),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var token = JwtHelper.GenerateToken(issuer, audience, key, claims);
        token.Should().NotBeNullOrWhiteSpace();

        var principal = JwtHelper.ValidateToken(token, issuer, audience, key);
        principal.Should().NotBeNull();
        principal!.FindFirst(ClaimTypes.NameIdentifier)!.Value.Should().Be("42");
        principal.IsInRole("Admin").Should().BeTrue();
    }
}
