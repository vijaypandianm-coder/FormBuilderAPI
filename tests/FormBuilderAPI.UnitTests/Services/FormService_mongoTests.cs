using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.EntityFrameworkCore;
using FormBuilderAPI.Application.Services;
using FormBuilderAPI.Application.Interfaces;
using FormBuilderAPI.UnitTests.TestUtils;
using FormBuilderAPI.DTOs;
using FormBuilderAPI.Models.MongoModels;
using FormBuilderAPI.Data;

public class ResponseAppServiceTests
{
    private static Form PublishedForm(int key = 7) => new()
    {
        Id = "f-id",
        FormKey = key,
        Status = "Published",
        Layout = new()
        {
            new FormSection
            {
                SectionId = "s",
                Fields = new()
                {
                    new FormField{ FieldId="age", Label="Age", Type="number", IsRequired=true },
                    new FormField{ FieldId="dept", Label="Dept", Type="dropdown", Options=new(){ new(){ Id="D1", Text="One"} } }
                }
            }
        }
    };

    [Fact]
    public async Task SubmitAsync_Creates_Header_And_Answers()
    {
        await using var db = InMemorySql.NewDb();
        var forms = new Mock<IFormService>();
        forms.Setup(f => f.GetByFormKeyAsync(7)).ReturnsAsync(PublishedForm(7));

        var app = new ResponseAppService(db, forms.Object);

        var payload = new SubmitResponseDto
        {
            Answers = new()
            {
                new(){ FieldId="age", AnswerValue="25" },
                new(){ FieldId="dept", OptionIds = new(){ "D1" } }
            }
        };

        await app.SubmitAsync(7, 100, payload);

        db.FormResponses.Should().HaveCount(1);
        db.FormResponseAnswers.Should().HaveCount(2);
        db.FormResponseAnswers.Select(a => a.FieldType).Should().Contain(new[]{ "number", "dropdown" });
    }

    [Fact]
    public async Task SubmitAsync_Validates_Required_And_Number()
    {
        await using var db = InMemorySql.NewDb();
        var forms = new Mock<IFormService>();
        forms.Setup(f => f.GetByFormKeyAsync(7)).ReturnsAsync(PublishedForm(7));

        var app = new ResponseAppService(db, forms.Object);

        // missing required
        var miss = new SubmitResponseDto{ Answers = new(){ new(){ FieldId="age", AnswerValue="" } } };
        await Assert.ThrowsAsync<InvalidOperationException>(() => app.SubmitAsync(7, 1, miss));

        // bad number
        var bad = new SubmitResponseDto{ Answers = new(){ new(){ FieldId="age", AnswerValue="abc" } } };
        await Assert.ThrowsAsync<InvalidOperationException>(() => app.SubmitAsync(7, 1, bad));
    }

    [Fact]
    public async Task List_And_Get_Work()
    {
        await using var db = InMemorySql.NewDb("resp-db");
        var forms = new Mock<IFormService>();
        forms.Setup(f => f.GetByFormKeyAsync(7)).ReturnsAsync(PublishedForm(7));

        var app = new ResponseAppService(db, forms.Object);

        await app.SubmitAsync(7, 100, new SubmitResponseDto{ Answers = new(){ new(){ FieldId="age", AnswerValue="30" } } });
        await app.SubmitAsync(7, 200, new SubmitResponseDto{ Answers = new(){ new(){ FieldId="age", AnswerValue="40" } } });

        (await app.ListAsync(7)).Should().HaveCount(2);
        (await app.ListAsync(7, 100)).Should().HaveCount(1);

        var first = (await app.ListAsync(7, 100)).Single();
        (await app.GetAsync(first.Id))!.UserId.Should().Be(100);
    }
}