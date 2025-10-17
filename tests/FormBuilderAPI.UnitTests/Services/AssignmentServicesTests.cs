using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

using FormBuilderAPI.Data;
using FormBuilderAPI.Models.SqlModels;
using FormBuilderAPI.Services;

namespace FormBuilderAPI.UnitTests.Services
{
    public class AssignmentServiceTests
    {
        private static SqlDbContext NewDb(string name)
        {
            var opts = new DbContextOptionsBuilder<SqlDbContext>()
                .UseInMemoryDatabase(name)
                .Options;
            return new SqlDbContext(opts);
        }

        [Fact]
        public async Task AssignAsync_first_assignment_gets_seq_1()
        {
            await using var db = NewDb(nameof(AssignAsync_first_assignment_gets_seq_1));
            var svc = new AssignmentService(db);

            var a = await svc.AssignAsync("form-1", 123);

            a.SequenceNo.Should().Be(1);
            a.FormId.Should().Be("form-1");
            a.UserId.Should().Be(123);
            (await db.FormAssignments.CountAsync()).Should().Be(1);
        }

        [Fact]
        public async Task AssignAsync_increments_sequence_per_form()
        {
            await using var db = NewDb(nameof(AssignAsync_increments_sequence_per_form));
            var svc = new AssignmentService(db);

            var a1 = await svc.AssignAsync("form-1", 1);
            var a2 = await svc.AssignAsync("form-1", 2);
            var b1 = await svc.AssignAsync("form-2", 1);

            a1.SequenceNo.Should().Be(1);
            a2.SequenceNo.Should().Be(2);
            b1.SequenceNo.Should().Be(1);
        }

        [Fact]
        public async Task AssignAsync_same_user_twice_throws()
        {
            await using var db = NewDb(nameof(AssignAsync_same_user_twice_throws));
            var svc = new AssignmentService(db);

            await svc.AssignAsync("form-1", 7);

            Func<Task> act = async () => await svc.AssignAsync("form-1", 7);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Already assigned*");
        }

        [Fact]
        public async Task UnassignAsync_removes_and_returns_true_false()
        {
            await using var db = NewDb(nameof(UnassignAsync_removes_and_returns_true_false));
            var svc = new AssignmentService(db);

            await svc.AssignAsync("form-1", 7);

            var removed = await svc.UnassignAsync("form-1", 7);
            removed.Should().BeTrue();
            (await db.FormAssignments.CountAsync()).Should().Be(0);

            var removedAgain = await svc.UnassignAsync("form-1", 7);
            removedAgain.Should().BeFalse();
        }

        [Fact]
        public async Task ListAssigneesAsync_returns_ordered_by_sequence()
        {
            await using var db = NewDb(nameof(ListAssigneesAsync_returns_ordered_by_sequence));
            var svc = new AssignmentService(db);

            var a1 = await svc.AssignAsync("form-1", 10); // seq 1
            var a2 = await svc.AssignAsync("form-1", 20); // seq 2
            var a3 = await svc.AssignAsync("form-1", 30); // seq 3

            var list = await svc.ListAssigneesAsync("form-1");
            list.Select(x => x.SequenceNo).Should().BeInAscendingOrder();
            list.Select(x => x.UserId).Should().ContainInOrder(10, 20, 30);
        }

        [Fact]
        public async Task IsUserAssignedAsync_and_HasAnyAssignmentsAsync_flags_work()
        {
            await using var db = NewDb(nameof(IsUserAssignedAsync_and_HasAnyAssignmentsAsync_flags_work));
            var svc = new AssignmentService(db);

            (await svc.HasAnyAssignmentsAsync("form-1")).Should().BeFalse();
            (await svc.IsUserAssignedAsync("form-1", 9)).Should().BeFalse();

            await svc.AssignAsync("form-1", 9);

            (await svc.HasAnyAssignmentsAsync("form-1")).Should().BeTrue();
            (await svc.IsUserAssignedAsync("form-1", 9)).Should().BeTrue();
        }
    }
}
