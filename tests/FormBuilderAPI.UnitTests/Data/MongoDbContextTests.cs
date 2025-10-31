using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;
using FormBuilderAPI.Data;

public class MongoDbContextTests
{
    [Fact]
    public void Can_Construct_And_Access_Collections()
    {
        var opts = Options.Create(new MongoDbSettings
        {
            ConnectionString = "mongodb://localhost:27017",
            DatabaseName = "unit_tests_db"
        });

        var ctx = new MongoDbContext(opts);

        ctx.Forms.Should().NotBeNull();
        ctx.Workflows.Should().NotBeNull();
        ctx.Counters.Should().NotBeNull();
    }
}