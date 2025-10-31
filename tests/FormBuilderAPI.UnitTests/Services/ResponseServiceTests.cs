using FluentAssertions;
using Moq;
using Xunit;
using FormBuilderAPI.Services;
using FormBuilderAPI.Application.Interfaces;
using FormBuilderAPI.DTOs;
using FormBuilderAPI.Models.MongoModels;
using FormBuilderAPI.Data;

public class ResponseServiceTests
{
    private static Form PublishedForm(int key = 4) => new()
    {
        Id = "mongo-id",
        FormKey = key,
        Status = "Published",
        Layout = new()
        {
            new FormSection
            {
                SectionId = "s1",
                Fields = new()
                {
                    new FormField { FieldId = "name", Label = "Name", Type = "shorttext", IsRequired = true },
                    new FormField { FieldId = "choice", Label = "Choice", Type = "dropdown", IsRequired = false,
                        Options = new(){ new FieldOption{ Id="A", Text="A"}, new FieldOption{ Id="B", Text="B"} } },
                    new FormField { FieldId = "file", Label="File", Type="file", IsRequired=false }
                }
            }
        }
    };

    [Fact]
    public async Task SaveAsync_Writes_Header_Answers_And_File()
    {
        var forms = new Mock<IFormService>();
        forms.Setup(f => f.GetByFormKeyAsync(4)).ReturnsAsync(PublishedForm(4));

        var repo = new Mock<IResponsesRepository>();
        repo.Setup(r => r.InsertFormResponseHeaderAsync(10, 4, "mongo-id"))
            .ReturnsAsync(111);
        repo.Setup(r => r.InsertFormResponseAnswerAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(1);
        repo.Setup(r => r.InsertFileAsync(111, 4, "file", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<byte[]>()))
            .ReturnsAsync(999);

        var svc = new ResponseService(forms.Object, repo.Object);

        var payload = new SubmitResponseDto
        {
            Answers = new()
            {
                new(){ FieldId="name", AnswerValue="Vijay" },
                new(){ FieldId="choice", OptionIds = new(){ "A", "B" } },
                new(){ FieldId="file", FileBase64 = "data:;base64,QQ==" } // single byte
            }
        };

        var id = await svc.SaveAsync(4, 10, payload);
        id.Should().Be(111);

        repo.Verify(r => r.InsertFileAsync(111, 4, "file", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<byte[]>()), Times.Once);
        repo.Verify(r => r.InsertFormResponseAnswerAsync(111, 10, 4, "choice", It.IsAny<string?>(), It.Is<string>(s => s!.Contains("["))), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_Throws_On_NotPublished_Or_Missing_Answers()
    {
        var forms = new Mock<IFormService>();
        forms.Setup(f => f.GetByFormKeyAsync(1)).ReturnsAsync(new Form { Id="x", FormKey=1, Status="Draft", Layout = new() });

        var repo = new Mock<IResponsesRepository>();
        var svc = new ResponseService(forms.Object, repo.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.SaveAsync(1, 1, new SubmitResponseDto{ Answers = new() }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.SaveAsync(1, 1, new SubmitResponseDto())); // null/empty
    }

    [Fact]
    public async Task ListAsync_Flattens_Headers_And_Answers()
    {
        var forms = new Mock<IFormService>();
        var repo = new Mock<IResponsesRepository>();

        repo.Setup(r => r.ListHeadersByFormKeyAsync(4))
            .ReturnsAsync(new List<ResponseHeaderDto> { new(){ Id=5, FormKey=4, UserId=10, SubmittedAt=DateTime.UtcNow }});
        repo.Setup(r => r.ListAnswersByResponseIdAsync(5))
            .ReturnsAsync(new List<ResponseAnswerRow> { new(){ FieldId="f1", AnswerValue="v1" }});

        var svc = new ResponseService(forms.Object, repo.Object);
        var rows = await svc.ListAsync(4);

        rows.Should().HaveCount(1);
        rows[0].FieldId.Should().Be("f1");
        rows[0].AnswerValue.Should().Be("v1");
    }

    [Fact]
    public async Task GetDetail_And_GetAsync_Work()
    {
        var repo = new Mock<IResponsesRepository>();
        var forms = new Mock<IFormService>();

        repo.Setup(r => r.GetHeaderByIdAsync(9)).ReturnsAsync(new ResponseHeaderDto{ Id=9, FormKey=4, UserId=1, SubmittedAt=DateTime.UtcNow });
        repo.Setup(r => r.ListAnswersByResponseIdAsync(9)).ReturnsAsync(new List<ResponseAnswerRow> { new(){ FieldId="x", FieldType="shortText", AnswerValue="ok" } });

        var svc = new ResponseService(forms.Object, repo.Object);

        var detail = await svc.GetDetailAsync(9);
        detail!.Header.Id.Should().Be(9);
        detail.Answers.Should().HaveCount(1);

        var flat = await svc.GetAsync(9);
        flat!.FieldId.Should().Be("x");
        (await svc.GetDetailAsync(999)).Should().BeNull();
        (await svc.GetAsync(999)).Should().BeNull();
    }
}