using FluentAssertions;
using Xunit;
using FormBuilderAPI.Services;
using FormBuilderAPI.Models.SqlModels;
using FormBuilderAPI.UnitTests.TestUtils;

public class AuditServiceTests
{
    [Fact]
    public async Task LogAsync_Writes_Row()
    {
        await using var db = InMemorySql.NewDb();
        var svc = new AuditService(db);

        await svc.LogAsync("Create", "Admin", "0", "F:1", "{\"x\":1}");

        db.AuditLogs.Should().HaveCount(1);
        var row = db.AuditLogs.Single();
        row.Action.Should().Be("Create");
        row.ActorRole.Should().Be("Admin");
        row.ActorUserId.Should().Be("0");
    }
}