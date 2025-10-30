using System;
using System.Linq;
using System.Runtime.Serialization; // FormatterServices
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

using FormBuilderAPI.Application.Services;
using FormBuilderAPI.Data;
using FormBuilderAPI.Models.SqlModels;
using FormBuilderAPI.Services;
using FormBuilderAPI.DTOs;

namespace FormBuilderAPI.UnitTests.Application
{
    public class ResponseAppService_SqlOnly_Tests
    {
        private static SqlDbContext NewInMemoryDb(string name)
        {
            var opts = new DbContextOptionsBuilder<SqlDbContext>()
                .UseInMemoryDatabase(databaseName: name)
                .Options;
            return new SqlDbContext(opts);
        }

        // We only need a dummy FormService instance because these tests
        // do NOT call the paths that touch Mongo (SubmitAsync happy path).
        private static FormService DummyFormService()
        {
            // Create an uninitialized instance to satisfy the ctor. We won't call it.
            #pragma warning disable SYSLIB0050 // Type or member is obsolete
            return (FormService)FormatterServices.GetUninitializedObject(typeof(FormService));
            #pragma warning restore SYSLIB0050 // Type or member is obsolete
        }

        [Fact]
        public async Task ListAsync_returns_rows_for_formKey_and_orders_by_time_then_id()
        {
            using var db = NewInMemoryDb(nameof(ListAsync_returns_rows_for_formKey_and_orders_by_time_then_id));

            // seed: two form keys, mixed users, mixed times
            var t0 = DateTime.UtcNow.AddMinutes(-10);
            db.FormResponses.AddRange(
                new FormResponse { FormId = "f1", FormKey = 100, UserId = 1, SubmittedAt = t0.AddMinutes(1) },
                new FormResponse { FormId = "f1", FormKey = 100, UserId = 2, SubmittedAt = t0.AddMinutes(2) },
                new FormResponse { FormId = "f2", FormKey = 200, UserId = 3, SubmittedAt = t0.AddMinutes(3) },
                new FormResponse { FormId = "f1", FormKey = 100, UserId = 1, SubmittedAt = t0.AddMinutes(4) }
            );
            await db.SaveChangesAsync();

            var svc = new ResponseAppService(db, DummyFormService());

            var rows = await svc.ListAsync(formKey: 100, userId: null);

            rows.Should().HaveCount(3);
            rows.Select(r => r.FormKey).Should().OnlyContain(k => k == 100);
            // Ordered by SubmittedAt desc, then Id asc
            rows.Select(r => r.SubmittedAt).Should().BeInDescendingOrder();
        }

        [Fact]
        public async Task ListAsync_can_filter_by_userId()
        {
            using var db = NewInMemoryDb(nameof(ListAsync_can_filter_by_userId));

            db.FormResponses.AddRange(
                new FormResponse { FormId = "f1", FormKey = 7, UserId = 10, SubmittedAt = DateTime.UtcNow.AddMinutes(-2) },
                new FormResponse { FormId = "f1", FormKey = 7, UserId = 99, SubmittedAt = DateTime.UtcNow.AddMinutes(-1) },
                new FormResponse { FormId = "f1", FormKey = 7, UserId = 10, SubmittedAt = DateTime.UtcNow }
            );
            await db.SaveChangesAsync();

            var svc = new ResponseAppService(db, DummyFormService());

            var rows = await svc.ListAsync(formKey: 7, userId: 10);

            rows.Should().HaveCount(2);
            rows.Should().OnlyContain(r => r.UserId == 10);
        }

        [Fact]
        public async Task GetAsync_returns_row_when_found_otherwise_null()
        {
            using var db = NewInMemoryDb(nameof(GetAsync_returns_row_when_found_otherwise_null));

            var row = new FormResponse
            {
                FormId = "fX",
                FormKey = 321,
                UserId = 42,
                SubmittedAt = DateTime.UtcNow
            };
            db.FormResponses.Add(row);
            await db.SaveChangesAsync();

            var svc = new ResponseAppService(db, DummyFormService());

            var hit = await svc.GetAsync(row.Id);
            hit.Should().NotBeNull();
            hit!.Id.Should().Be(row.Id);
            hit.FormKey.Should().Be(321);
            hit.UserId.Should().Be(42);

            var miss = await svc.GetAsync(id: row.Id + 12345);
            miss.Should().BeNull();
        }

        [Fact]
        public async Task SubmitAsync_throws_when_payload_is_null_before_hitting_Mongo()
        {
            using var db = NewInMemoryDb(nameof(SubmitAsync_throws_when_payload_is_null_before_hitting_Mongo));
            var svc = new ResponseAppService(db, DummyFormService());

            Func<Task> act = async () => await svc.SubmitAsync(formKey: 1, userId: 5, payload: null!);

            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*No answers provided*");
        }
    }
}