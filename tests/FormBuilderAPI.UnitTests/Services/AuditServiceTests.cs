
using System.Threading.Tasks;
using FluentAssertions;
using FormBuilderAPI.Services;
using FormBuilderAPI.UnitTests.Common;
using Xunit;

namespace FormBuilderAPI.UnitTests.Services;

public class AuditServiceTests
{
    [Fact]
    public async Task LogAsync_writes_a_row()
    {
        using var db = TestDb.Create();
        var sut = new AuditService(db);

        await sut.LogAsync(
            action: "Create",
            actorRole: "Admin",
            actorId: "42",
            entityId: "FORM-1",
            detailsJson: "{\"ok\":true}"
        );

        db.AuditLogs.Should().HaveCount(1);
        var row = await db.AuditLogs.FindAsync(db.AuditLogs.First().Id);
        row!.Action.Should().Be("Create");
        row.ActorRole.Should().Be("Admin");
        row.ActorUserId.Should().Be("42");
        row.EntityId.Should().Be("FORM-1");
        row.DetailsJson.Should().Contain("ok");
    }
}
