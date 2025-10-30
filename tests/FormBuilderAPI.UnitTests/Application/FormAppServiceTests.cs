// File: FormBuilder.Tests/FormAppServiceTests.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

using FormBuilderAPI.Application.Services;
using FormBuilderAPI.Application.Interfaces;
using FormBuilderAPI.Data;
using FormBuilderAPI.DTOs;
using FormBuilderAPI.Models.MongoModels;
using FormBuilderAPI.Models.SqlModels;
using FormBuilderAPI.Services;

public class FormAppServiceTests
{
    private static SqlDbContext NewDb(string dbName)
    {
        var opts = new DbContextOptionsBuilder<SqlDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new SqlDbContext(opts);
    }

    private static Form NewMongo(string id = "f1", int? formKey = 100, string status = "Draft")
        => new Form
        {
            Id = id,
            FormKey = formKey,
            Title = "T",
            Description = "D",
            Status = status,
            Access = "Open",
            CreatedBy = "me",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddDays(-2),
            Layout = new List<FormSection>()
        };

    [Fact]
    public async Task CreateMetaAsync_Creates_FormKey_And_Syncs_Back()
    {
        using var db = NewDb(nameof(CreateMetaAsync_Creates_FormKey_And_Syncs_Back));

        var createdForm = NewMongo(id: "mongo-new", formKey: null);
        var forms = new Mock<IFormService>();
        forms.Setup(f => f.CreateFormAsync(It.IsAny<Form>()))
             .ReturnsAsync((Form f) => { f.Id = createdForm.Id; return f; });
        forms.Setup(f => f.UpdateFormAsync(createdForm.Id, It.IsAny<Form>()))
             .ReturnsAsync((string _, Form f) => f);

        var assign = new Mock<AssignmentService>(null!) { CallBase = false };

        var svc = new FormAppService(forms.Object, db, assign.Object);

        var meta = new FormMetaDto { Title = "Meta T", Description = "Meta D" };
        var outDto = await svc.CreateMetaAsync("author", meta);

        // SQL key row created
        db.FormKeys.Count().Should().Be(1);
        var key = db.FormKeys.Single();

        outDto.FormKey.Should().Be(key.Id);
        outDto.Title.Should().Be("Meta T");

        // Mongo was updated with numeric key
        forms.Verify(f => f.UpdateFormAsync(createdForm.Id, It.Is<Form>(x => x.FormKey == key.Id)), Times.Once);
    }

    [Fact]
    public async Task UpdateMetaAsync_Updates_When_Draft_Else_Throws()
    {
        using var db = NewDb(nameof(UpdateMetaAsync_Updates_When_Draft_Else_Throws));
        var draft = NewMongo(status: "Draft");
        var published = NewMongo(id: "f2", status: "Published");

        var forms = new Mock<IFormService>();
        forms.Setup(f => f.GetByFormKeyAsync(100)).ReturnsAsync(draft);
        forms.Setup(f => f.GetByFormKeyAsync(200)).ReturnsAsync(published);
        forms.Setup(f => f.UpdateFormAsync(draft.Id, It.IsAny<Form>()))
             .ReturnsAsync((string _, Form f) => f);

        var svc = new FormAppService(forms.Object, db, new Mock<AssignmentService>(null!).Object);

        var res = await svc.UpdateMetaAsync(100, new FormMetaDto { Title = "New", Description = "Nd" });
        res.Title.Should().Be("New");

        await FluentActions.Invoking(() => svc.UpdateMetaAsync(200, new FormMetaDto { Title = "X" }))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Published*cannot be edited*");
    }

    [Fact]
    public async Task AddLayoutAsync_And_SetLayoutAsync_Work_And_Generate_IDs()
    {
        using var db = NewDb(nameof(AddLayoutAsync_And_SetLayoutAsync_Work_And_Generate_IDs));
        var form = NewMongo(formKey: 111, status: "Draft");
        var forms = new Mock<IFormService>();
        forms.Setup(f => f.GetByFormKeyAsync(111)).ReturnsAsync(form);
        forms.Setup(f => f.UpdateFormAsync(form.Id, It.IsAny<Form>()))
             .ReturnsAsync((string _, Form f) => f);

        var svc = new FormAppService(forms.Object, db, new Mock<AssignmentService>(null!).Object);

        var layout = new FormLayoutDto
        {
            Sections = new()
            {
                new()
                {
                    Title = "S1",
                    Fields = new()
                    {
                        new() { Label = "Q1", Type = "radio", IsRequired = true, Options = new() { "Yes", "No" } },
                        new() { Label = "Q2", Type = "shortText", IsRequired = false }
                    }
                }
            }
        };

        var added = await svc.AddLayoutAsync(111, layout);
        added.Layout.Should().NotBeNull();
        added.Layout!.Single().Fields.Should().HaveCount(2);
        added.Layout!.Single().Fields[0].Options.Should().NotBeNull();
        added.Layout!.Single().Fields[1].Options.Should().BeNull(); // non-choice

        // Replace whole layout
        var replaced = await svc.SetLayoutAsync(111, new FormLayoutDto { Sections = new() { layout.Sections[0] } });
        replaced.Layout!.Single().Fields.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByKeyAsync_Respects_Draft_Visibility()
    {
        using var db = NewDb(nameof(GetByKeyAsync_Respects_Draft_Visibility));
        var draft = NewMongo(status: "Draft");
        var forms = new Mock<IFormService>();
        forms.Setup(f => f.GetByFormKeyAsync(100)).ReturnsAsync(draft);

        var svc = new FormAppService(forms.Object, db, new Mock<AssignmentService>(null!).Object);

        // not visible
        await FluentActions.Invoking(() => svc.GetByKeyAsync(100, allowPreview: false, isAdmin: false))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*not visible*");

        // visible if allowPreview
        var r1 = await svc.GetByKeyAsync(100, allowPreview: true, isAdmin: false);
        r1.Id.Should().Be(draft.Id);

        // visible if admin
        var r2 = await svc.GetByKeyAsync(100, allowPreview: false, isAdmin: true);
        r2.Id.Should().Be(draft.Id);
    }

    [Fact]
    public async Task ListAsync_Pipes_Through()
    {
        using var db = NewDb(nameof(ListAsync_Pipes_Through));
        var f1 = NewMongo(id: "A"); var f2 = NewMongo(id: "B");
        var forms = new Mock<IFormService>();
        forms.Setup(f => f.ListAsync(null, null, true, 1, 10))
             .ReturnsAsync((new List<Form> { f1, f2 }, 2));

        var svc = new FormAppService(forms.Object, db, new Mock<AssignmentService>(null!).Object);
        var (items, total) = await svc.ListAsync(null, true, 1, 10);

        total.Should().Be(2);
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task SetStatusAsync_Validates_And_SelfHeals_FormKey_On_Publish()
    {
        using var db = NewDb(nameof(SetStatusAsync_Validates_And_SelfHeals_FormKey_On_Publish));
        var draftNoKey = NewMongo(id: "f1", formKey: null, status: "Draft");
        var alreadyPublished = NewMongo(id: "f2", status: "Published");

        var forms = new Mock<IFormService>();
        forms.Setup(f => f.GetByFormKeyAsync(123)).ReturnsAsync(draftNoKey);
        forms.Setup(f => f.GetByFormKeyAsync(456)).ReturnsAsync(alreadyPublished);
        forms.Setup(f => f.UpdateFormAsync(It.IsAny<string>(), It.IsAny<Form>()))
             .ReturnsAsync((string _, Form f) => f);

        var svc = new FormAppService(forms.Object, db, new Mock<AssignmentService>(null!).Object);

        // invalid status
        await FluentActions.Invoking(() => svc.SetStatusAsync(123, "X"))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Draft or Published*");

        // publish path + self-heal creates SQL key row, syncs numeric key
        var published = await svc.SetStatusAsync(123, "Published");
        db.FormKeys.Count().Should().Be(1);
        published.Status.Should().Be("Published");
        published.FormKey.Should().NotBeNull();

        // cannot change Published
        await FluentActions.Invoking(() => svc.SetStatusAsync(456, "Draft"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already Published*");
    }

    [Fact]
    public async Task SetAccessAsync_Validates_And_Updates()
    {
        using var db = NewDb(nameof(SetAccessAsync_Validates_And_Updates));
        var draft = NewMongo(status: "Draft");
        var forms = new Mock<IFormService>();
        forms.Setup(f => f.GetByFormKeyAsync(100)).ReturnsAsync(draft);
        forms.Setup(f => f.UpdateFormAsync(draft.Id, It.IsAny<Form>()))
             .ReturnsAsync((string _, Form f) => f);

        var svc = new FormAppService(forms.Object, db, new Mock<AssignmentService>(null!).Object);

        // invalid
        await FluentActions.Invoking(() => svc.SetAccessAsync(100, "Private"))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Open or Restricted*");

        var ok = await svc.SetAccessAsync(100, "Restricted");
        ok.Access.Should().Be("Restricted");

        // cannot change access if Published
        var published = NewMongo(id: "f2", status: "Published");
        forms.Setup(f => f.GetByFormKeyAsync(200)).ReturnsAsync(published);

        await FluentActions.Invoking(() => svc.SetAccessAsync(200, "Open"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Published*cannot change access*");
    }

    [Fact]
    public async Task DeleteAsync_Uses_Repository_And_Throws_When_NotFound()
    {
        using var db = NewDb(nameof(DeleteAsync_Uses_Repository_And_Throws_When_NotFound));
        var form = NewMongo(status: "Draft");
        var forms = new Mock<IFormService>();
        forms.Setup(f => f.GetByFormKeyAsync(100)).ReturnsAsync(form);
        forms.Setup(f => f.DeleteFormAndResponsesAsync(form.Id)).ReturnsAsync(true);

        var svc = new FormAppService(forms.Object, db, new Mock<AssignmentService>(null!).Object);

        await svc.DeleteAsync(100);

        // not found path
        var form2 = NewMongo(id: "f2", status: "Draft");
        forms.Setup(f => f.GetByFormKeyAsync(200)).ReturnsAsync(form2);
        forms.Setup(f => f.DeleteFormAndResponsesAsync(form2.Id)).ReturnsAsync(false);

        await FluentActions.Invoking(() => svc.DeleteAsync(200))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*not found*");
    }

    [Fact]
    
public async Task Assign_Unassign_ListAssignees_Work_And_Map()
{
    using var db = NewDb(nameof(Assign_Unassign_ListAssignees_Work_And_Map));
    var form = NewMongo(status: "Draft");
    var forms = new Mock<IFormService>();
    forms.Setup(f => f.GetByFormKeyAsync(100)).ReturnsAsync(form);

    // Mock AssignmentService
    var assign = new Mock<AssignmentService>(null!);

    assign.Setup(a => a.AssignAsync(It.IsAny<string>(), It.IsAny<long>()))
          .ReturnsAsync(new FormAssignment
          {
              Id = 99,
              SequenceNo = 0,
              FormId = form.Id,
              UserId = 9,
              AssignedAt = DateTime.UtcNow
          });

    assign.Setup(a => a.UnassignAsync(form.Id, 9)).ReturnsAsync(true);
    assign.Setup(a => a.UnassignAsync(form.Id, 8)).ReturnsAsync(false);

    assign.Setup(a => a.ListAssigneesAsync(form.Id))
          .ReturnsAsync(new List<FormAssignment>
          {
              new() { Id = 1, SequenceNo = 0, FormId = form.Id, UserId = 9,  AssignedAt = DateTime.UtcNow },
              new() { Id = 2, SequenceNo = 7, FormId = form.Id, UserId = 11, AssignedAt = DateTime.UtcNow },
          });

    var svc = new FormAppService(forms.Object, db, assign.Object);

    // Assign path
    await svc.AssignUserAsync(100, 9);
    assign.Verify(a => a.AssignAsync(form.Id, 9), Times.Once);

    // List mapping (anonymous projection)
    var list = await svc.ListAssigneesAsync(100);
    list.Should().HaveCount(2);
    list.Select(x => x.GetType().GetProperty("userId")!.GetValue(x))
        .Should().BeEquivalentTo(new object[] { 9L, 11L });

    // Unassign OK
    await svc.UnassignUserAsync(100, 9);
    assign.Verify(a => a.UnassignAsync(form.Id, 9), Times.Once);

    // Unassign not found -> throws
    await FluentActions.Invoking(() => svc.UnassignUserAsync(100, 8))
        .Should().ThrowAsync<KeyNotFoundException>()
        .WithMessage("*Assignment not found*");
}
}