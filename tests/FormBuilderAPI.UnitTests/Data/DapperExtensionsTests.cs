using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using FormBuilderAPI.Data;
using FormBuilderAPI.UnitTests.TestUtils;

public class DapperExtensionsTests
{
    [Fact]
    public async Task WithConn_T_Returns_Value_And_Opens_Connection()
    {
        var fakeConn = new FakeDbConnection();
        var factory = new FakeDbConnectionFactory(fakeConn);

        var result = await factory.WithConn(async conn =>
        {
            conn.State.Should().Be(System.Data.ConnectionState.Open);
            return 42;
        });

        result.Should().Be(42);
        fakeConn.Opens.Should().Be(1);
    }

    [Fact]
    public async Task WithConn_Void_Opens_Connection()
    {
        var fakeConn = new FakeDbConnection();
        var factory = new FakeDbConnectionFactory(fakeConn);

        var hit = false;
        await factory.WithConn(async conn =>
        {
            conn.State.Should().Be(System.Data.ConnectionState.Open);
            hit = true;
        });

        hit.Should().BeTrue();
        fakeConn.Opens.Should().Be(1);
    }
}