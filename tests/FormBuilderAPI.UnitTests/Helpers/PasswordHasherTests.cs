using FluentAssertions;
using Xunit;
using FormBuilderAPI.Helpers;

namespace FormBuilderAPI.UnitTests.Helpers;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_And_Verify_Works()
    {
        var plain = "Vijay@123";
        var hash = PasswordHasher.Hash(plain);

        hash.Should().NotBeNullOrWhiteSpace();
        PasswordHasher.Verify(plain, hash).Should().BeTrue();
        PasswordHasher.Verify("nope", hash).Should().BeFalse();
    }
}
