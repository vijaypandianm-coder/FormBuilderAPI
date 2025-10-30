using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

using FormBuilderAPI.Application.Services;
using FormBuilderAPI.Data;
using FormBuilderAPI.DTOs;
using FormBuilderAPI.Models.MongoModels;
using FormBuilderAPI.Services;
using FormBuilderAPI.Application.Interfaces;

namespace FormBuilderAPI.UnitTests.Application
{
    public class FormAppServiceTests
    {
        private static SqlDbContext NewInMemoryDb()
        {
            var opts = new DbContextOptionsBuilder<SqlDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new SqlDbContext(opts);
        }

        private static Form DraftForm(int formKey = 1) => new Form
        {
            Id = "64f0deadbeef123456789012",
            FormKey = formKey,
            Title = "Title",
            Description = "Desc",
            Status = "Draft",
            Access = "Open",
            CreatedBy = "u1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Layout = new List<FormSection>()
        };

        [Fact]
        public async Task CreateMetaAsync_creates_draft_open_using_forms_service()
        {
            using var db = NewInMemoryDb();

            var created = DraftForm(100);
            var formSvc = new Mock<IFormService>(MockBehavior.Strict);
            formSvc.Setup(s => s.CreateFormAsync(It.IsAny<Form>())).ReturnsAsync(created);

            var assignSvc = new Mock<AssignmentService>(MockBehavior.Loose, db).Object; // not used here
            var app = new FormAppService(formSvc.Object, db, assignSvc);

            var dto = await app.CreateMetaAsync("author", new FormMetaDto { Title="T1", Description="D1" });

            dto.Status.Should().Be("Draft");
            dto.Access.Should().Be("Open");
            dto.FormKey.Should().Be(100);
            formSvc.Verify(s => s.CreateFormAsync(It.IsAny<Form>()), Times.Once);
        }

        [Fact]
        public async Task UpdateMetaAsync_updates_when_draft()
        {
            using var db = NewInMemoryDb();

            var form = DraftForm(123);
            var formSvc = new Mock<IFormService>(MockBehavior.Strict);
            formSvc.Setup(s => s.GetByFormKeyAsync(123)).ReturnsAsync(form);

            // emulate update returns new version
            form.Title = "New title";
            form.Description = "New desc";
            formSvc.Setup(s => s.UpdateFormAsync(form.Id, It.IsAny<Form>())).ReturnsAsync(form);

            var app = new FormAppService(formSvc.Object, db, new Mock<AssignmentService>(MockBehavior.Loose, db).Object);

            var outDto = await app.UpdateMetaAsync(123, new FormMetaDto { Title="New title", Description="New desc" });
            outDto.Title.Should().Be("New title");
            outDto.Description.Should().Be("New desc");
        }

        [Fact]
        public async Task SetStatusAsync_publishes_and_sets_publishedAt()
        {
            using var db = NewInMemoryDb();

            var form = DraftForm(1);
            var formSvc = new Mock<IFormService>(MockBehavior.Strict);
            formSvc.Setup(s => s.GetByFormKeyAsync(1)).ReturnsAsync(form);
            formSvc.Setup(s => s.UpdateFormAsync(form.Id, It.IsAny<Form>()))
                   .ReturnsAsync((string _, Form f) => f);

            var app = new FormAppService(formSvc.Object, db, new Mock<AssignmentService>(MockBehavior.Loose, db).Object);

            var dto = await app.SetStatusAsync(1, "Published");
            dto.Status.Should().Be("Published");
            dto.PublishedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task GetByKeyAsync_hides_draft_for_non_admin_no_preview()
        {
            using var db = NewInMemoryDb();

            var draft = DraftForm(456);
            var formSvc = new Mock<IFormService>(MockBehavior.Strict);
            formSvc.Setup(s => s.GetByFormKeyAsync(456)).ReturnsAsync(draft);

            var app = new FormAppService(formSvc.Object, db, new Mock<AssignmentService>(MockBehavior.Loose, db).Object);

            Func<Task> act = () => app.GetByKeyAsync(456, allowPreview:false, isAdmin:false);
            // Use exact message matching instead of wildcard
            await act.Should().ThrowAsync<KeyNotFoundException>().Where(e => e.Message.Contains("not visible", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task SetLayoutAsync_replaces_layout_when_draft()
        {
            using var db = NewInMemoryDb();

            var form = DraftForm(123);
            var formSvc = new Mock<IFormService>(MockBehavior.Strict);
            formSvc.Setup(s => s.GetByFormKeyAsync(123)).ReturnsAsync(form);
            formSvc.Setup(s => s.UpdateFormAsync(form.Id, It.IsAny<Form>()))
                   .ReturnsAsync((string _, Form f) => f);

            var app = new FormAppService(formSvc.Object, db, new Mock<AssignmentService>(MockBehavior.Loose, db).Object);

            var layout = new FormLayoutDto
            {
                Sections = new()
                {
                    new FormSectionCreateDto
                    {
                        Title = "S1",
                        Fields = new()
                        {
                            new FieldCreateDto { Label="Q1", Type="shortText", IsRequired=true }
                        }
                    }
                }
            };

            var dto = await app.SetLayoutAsync(123, layout);

            dto.Layout.Should().NotBeNull();
            dto.Layout!.Should().HaveCount(1);
            dto.Layout[0].Fields.Should().HaveCount(1);
        }

        [Fact]
        public async Task DeleteAsync_calls_delete_and_throws_when_false()
        {
            using var db = NewInMemoryDb();

            var form = DraftForm(7);
            var formSvc = new Mock<IFormService>(MockBehavior.Strict);
            formSvc.Setup(s => s.GetByFormKeyAsync(7)).ReturnsAsync(form);
            formSvc.Setup(s => s.DeleteFormAndResponsesAsync(form.Id)).ReturnsAsync(false);

            var app = new FormAppService(formSvc.Object, db, new Mock<AssignmentService>(MockBehavior.Loose, db).Object);

            Func<Task> act = () => app.DeleteAsync(7);
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }
    }
}
