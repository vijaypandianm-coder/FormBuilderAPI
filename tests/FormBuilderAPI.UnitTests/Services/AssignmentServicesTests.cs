using FluentAssertions;
using Xunit;
using FormBuilderAPI.Services;
using FormBuilderAPI.Models.SqlModels;
using FormBuilderAPI.UnitTests.TestUtils;

public class AssignmentServiceTests
{
    [Fact]
    public async Task AssignAsync_Assigns_With_Incrementing_Sequence()
    {
        await using var db = InMemorySql.NewDb();
        var svc = new AssignmentService(db);

        var a1 = await svc.AssignAsync("form-1", 1001);
        var a2 = await svc.AssignAsync("form-1", 1002);

        a1.SequenceNo.Should().Be(1);
        a2.SequenceNo.Should().Be(2);
        (await db.FormAssignments.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task AssignAsync_Is_Idempotent_Per_User()
    {
        await using var db = InMemorySql.NewDb();
        var svc = new AssignmentService(db);

        await svc.AssignAsync("f", 7);
        var act = () => svc.AssignAsync("f", 7);
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*Already assigned*");
    }

    [Fact]
    public async Task Unassign_List_IsAssigned_HasAny()
    {
        await using var db = InMemorySql.NewDb();
        var svc = new AssignmentService(db);

        await svc.AssignAsync("form-x", 1);
        await svc.AssignAsync("form-x", 2);

        (await svc.IsUserAssignedAsync("form-x", 2)).Should().BeTrue();
        (await svc.HasAnyAssignmentsAsync("form-x")).Should().BeTrue();

        var list = await svc.ListAssigneesAsync("form-x");
        list.Select(x => x.SequenceNo).Should().BeInAscendingOrder();

        (await svc.UnassignAsync("form-x", 2)).Should().BeTrue();
        (await svc.UnassignAsync("form-x", 9)).Should().BeFalse();
    }
}