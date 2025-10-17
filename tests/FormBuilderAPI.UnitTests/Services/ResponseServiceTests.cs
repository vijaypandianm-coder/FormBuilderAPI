using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FormBuilderAPI.Models.SqlModels;
using FormBuilderAPI.Services;
using FormBuilderAPI.UnitTests.Common;
using Xunit;

namespace FormBuilderAPI.UnitTests.Services;

public class ResponseServiceTests
{
    [Fact]
    public async Task ListAsync_returns_flat_rows_for_form_and_user()
    {
        using var db = TestDb.Create();

        // Seed one header + two answers
        var header = new FormResponse
        {
            Id = 1001,
            FormId = "mongo-1",
            FormKey = 77,
            UserId = 555,
            SubmittedAt = DateTime.UtcNow.AddMinutes(-1)
        };
        db.FormResponses.Add(header);

        db.FormResponseAnswers.Add(new FormResponseAnswer
        {
            Id = 2001, ResponseId = 1001, FormKey = 77, UserId = 555,
            FieldId = "q1", FieldType = "text", AnswerValue = "hello",
            SubmittedAt = DateTime.UtcNow
        });
        db.FormResponseAnswers.Add(new FormResponseAnswer
        {
            Id = 2002, ResponseId = 1001, FormKey = 77, UserId = 555,
            FieldId = "q2", FieldType = "number", AnswerValue = "10",
            SubmittedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new ResponseService(db, forms: null!);

        // Act
        var rows = await sut.ListAsync(formKey: 77, userId: 555);

        // Assert
        rows.Should().HaveCount(2);
        rows.Select(r => r.FieldId).Should().BeEquivalentTo(new[] { "q1", "q2" });
        rows.All(r => r.ResponseId == 1001).Should().BeTrue();
    }

    [Fact]
    public async Task GetAsync_returns_row_by_answer_id()
    {
        using var db = TestDb.Create();

        db.FormResponseAnswers.Add(new FormResponseAnswer
        {
            Id = 3001, ResponseId = 123, FormKey = 80, UserId = 9,
            FieldId = "f1", FieldType = "text", AnswerValue = "abc",
            SubmittedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new ResponseService(db, forms: null!);

        var dto = await sut.GetAsync(3001);

        dto.Should().NotBeNull();
        dto!.FormKey.Should().Be(80);
        dto.FieldId.Should().Be("f1");
        dto.AnswerValue.Should().Be("abc");
    }
}