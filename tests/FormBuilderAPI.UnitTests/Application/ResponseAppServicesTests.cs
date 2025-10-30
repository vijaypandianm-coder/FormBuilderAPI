// File: tests/FormBuilderAPI.UnitTests/Application/ResponseAppServiceTests.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

using FormBuilderAPI.Application.Services;
using FormBuilderAPI.Data;
using FormBuilderAPI.DTOs;
using FormBuilderAPI.Models.MongoModels;
using FormBuilderAPI.Models.SqlModels;
using FormBuilderAPI.Application.Interfaces;

namespace FormBuilderAPI.UnitTests.Application
{
    public class ResponseAppServiceTests
    {
        /// <summary>
        /// Creates a relational, in-memory SQLite database so that:
        /// - Transactions work
        /// - ExecuteSqlRawAsync works
        /// - Dapper/raw SQL from your service works
        /// We keep the connection open for the duration of the context's lifetime.
        /// </summary>
        private static SqlDbContext NewRelationalInMemoryDb()
        {
            // One open connection keeps the in-memory DB alive for this DbContext
            var conn = new SqliteConnection("DataSource=:memory:");
            conn.Open();

            var opts = new DbContextOptionsBuilder<SqlDbContext>()
                .UseSqlite(conn)
                .EnableSensitiveDataLogging()
                .Options;

            var ctx = new SqlDbContext(opts);
            ctx.Database.EnsureCreated(); // build schema for the EF sets used in tests
            return ctx; // note: connection will be closed when test process ends; safe for unit tests
        }

        private static Form BuildPublishedForm(int formKey) => new Form
        {
            Id        = "64f0c0ffee1234567890abcd",
            FormKey   = formKey,
            Title     = "Sample Form",
            Status    = "Published",
            Access    = "Open",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Layout = new List<FormSection>
            {
                new FormSection
                {
                    SectionId = "sec-1",
                    Title = "Section",
                    Fields = new List<FormField>
                    {
                        new FormField { FieldId="f1", Label="Name",  Type="shortText", IsRequired=true  },
                        new FormField { FieldId="f2", Label="Age",   Type="number",    IsRequired=false },
                        new FormField
                        {
                            FieldId="f3", Label="Color", Type="radio", IsRequired=true,
                            Options = new List<FieldOption>
                            {
                                new FieldOption{ Id="red", Text="Red"  },
                                new FieldOption{ Id="blu", Text="Blue" }
                            }
                        }
                    }
                }
            }
        };

        [Fact]
        public async Task SubmitAsync_persists_one_header_and_N_answers_when_published()
        {
            using var db = NewRelationalInMemoryDb();

            var form = BuildPublishedForm(99);
            var formsMock = new Mock<IFormService>(MockBehavior.Strict);
            formsMock.Setup(s => s.GetByFormKeyAsync(99)).ReturnsAsync(form);

            var svc = new ResponseAppService(db, formsMock.Object);

            var payload = new SubmitResponseDto
            {
                Answers = new List<SubmitAnswerDto>
                {
                    new() { FieldId = "f1", AnswerValue = "Alice" },
                    new() { FieldId = "f2", AnswerValue = "42"    },
                    new() { FieldId = "f3", OptionIds   = new List<string>{ "blu" } }
                }
            };

            await svc.SubmitAsync(99, userId: 777, payload);

            // Expect ONE header row
            var headers = await db.FormResponses.AsNoTracking().ToListAsync();
            headers.Should().HaveCount(1);
            headers.Single().Should().BeEquivalentTo(new
            {
                FormKey = 99,
                UserId  = 777L,
                FormId  = form.Id
            }, o => o.ExcludingMissingMembers());

            // Expect N answer rows
            var answers = await db.FormResponseAnswers.AsNoTracking().ToListAsync();
            answers.Should().HaveCount(payload.Answers.Count);
            answers.All(a => a.FormKey == 99 && a.UserId == 777 && a.ResponseId == headers.Single().Id)
                   .Should().BeTrue();

            answers.Select(a => a.FieldId).Should().BeEquivalentTo(new[] { "f1", "f2", "f3" });
        }

        [Fact]
        public async Task SubmitAsync_throws_if_form_not_published()
        {
            using var db = NewRelationalInMemoryDb();

            var draft = BuildPublishedForm(11);
            draft.Status = "Draft";

            var formsMock = new Mock<IFormService>(MockBehavior.Strict);
            formsMock.Setup(s => s.GetByFormKeyAsync(11)).ReturnsAsync(draft);

            var svc = new ResponseAppService(db, formsMock.Object);

            var payload = new SubmitResponseDto
            {
                Answers = new List<SubmitAnswerDto>
                {
                    new() { FieldId="f1", AnswerValue="x" }
                }
            };

            Func<Task> act = () => svc.SubmitAsync(11, 1, payload);

            // Be permissive on message (case-insensitive, substring)
            await act.Should().ThrowAsync<InvalidOperationException>()
                .Where(ex => ex.Message.Contains("not published", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task SubmitAsync_throws_when_answers_missing()
        {
            using var db = NewRelationalInMemoryDb();

            var form = BuildPublishedForm(55);
            var formsMock = new Mock<IFormService>(MockBehavior.Strict);
            formsMock.Setup(s => s.GetByFormKeyAsync(55)).ReturnsAsync(form);

            var svc = new ResponseAppService(db, formsMock.Object);

            Func<Task> act = () => svc.SubmitAsync(55, 1, new SubmitResponseDto { Answers = new() });

            await act.Should().ThrowAsync<ArgumentException>()
                .Where(ex => ex.Message.Contains("No answers provided", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task SubmitAsync_throws_when_required_choice_missing()
        {
            using var db = NewRelationalInMemoryDb();

            var form = BuildPublishedForm(77);
            var formsMock = new Mock<IFormService>(MockBehavior.Strict);
            formsMock.Setup(s => s.GetByFormKeyAsync(77)).ReturnsAsync(form);

            var svc = new ResponseAppService(db, formsMock.Object);

            var payload = new SubmitResponseDto
            {
                Answers = new List<SubmitAnswerDto>
                {
                    new() { FieldId="f1", AnswerValue="Bob" },
                    // f3 is required radio; empty OptionIds -> should throw
                    new() { FieldId="f3", OptionIds = new List<string>() }
                }
            };

            Func<Task> act = () => svc.SubmitAsync(77, 2, payload);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .Where(ex => ex.Message.Contains("required", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task ListAsync_filters_by_formKey_and_optional_userId()
        {
            using var db = NewRelationalInMemoryDb();

            db.FormResponses.AddRange(
                new FormResponse { FormId="x", FormKey=5, UserId=1, SubmittedAt=DateTime.UtcNow },
                new FormResponse { FormId="x", FormKey=5, UserId=2, SubmittedAt=DateTime.UtcNow },
                new FormResponse { FormId="x", FormKey=6, UserId=1, SubmittedAt=DateTime.UtcNow }
            );
            await db.SaveChangesAsync();

            var dummyFormSvc = new Mock<IFormService>().Object;
            var svc = new ResponseAppService(db, dummyFormSvc);

            var allFor5 = await svc.ListAsync(5, null);
            allFor5.Should().HaveCount(2);

            var onlyUser1 = await svc.ListAsync(5, 1);
            onlyUser1.Should().HaveCount(1);
            onlyUser1.Single().UserId.Should().Be(1);
        }

        [Fact]
        public async Task GetAsync_returns_single_row()
        {
            using var db = NewRelationalInMemoryDb();

            var row = new FormResponse { FormId="x", FormKey=9, UserId=1, SubmittedAt=DateTime.UtcNow };
            db.FormResponses.Add(row);
            await db.SaveChangesAsync();

            var svc = new ResponseAppService(db, new Mock<IFormService>().Object);

            var found = await svc.GetAsync(row.Id);
            found.Should().NotBeNull();
            found!.Id.Should().Be(row.Id);
        }
    }
}