using System.Security.Claims;
using FluentAssertions;
using FormBuilderAPI.Application.Interfaces;
using FormBuilderAPI.Controllers;
using FormBuilderAPI.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace FormBuilderAPI.UnitTests.Controllers
{
    public class FormsControllerTests
    {
        private static FormsController BuildController(
            Mock<IFormAppService> appMock,
            bool isAdmin = true,
            string? userId = "u-123")
        {
            var controller = new FormsController(appMock.Object);

            var claims = new List<Claim>();
            if (!string.IsNullOrEmpty(userId))
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            if (isAdmin)
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            return controller;
        }

        [Fact]
        public async Task CreateMeta_Returns_CreatedAtAction_With_FormKey()
        {
            // Arrange
            var app = new Mock<IFormAppService>();
            var returned = new FormOutDto { FormKey = 42 };
            app.Setup(a => a.CreateMetaAsync(It.IsAny<string>(), It.IsAny<FormMetaDto>()))
               .ReturnsAsync(returned);

            var controller = BuildController(app);

            // Act
            var result = await controller.CreateMeta(new FormMetaDto { Title = "t" });

            // Assert
            var created = result as CreatedAtActionResult;
            created.Should().NotBeNull();
            created!.ActionName.Should().Be(nameof(FormsController.GetByKey));
            created.RouteValues.Should().ContainKey("formKey")
                   .WhoseValue.Should().Be(42);
            created.Value.Should().Be(returned);

            app.Verify(a => a.CreateMetaAsync("u-123", It.IsAny<FormMetaDto>()), Times.Once);
        }

        [Fact]
        public async Task UpdateMeta_Returns_Ok_With_Dto()
        {
            var app = new Mock<IFormAppService>();
            var dto = new FormOutDto { FormKey = 7, Title = "new" };
            app.Setup(a => a.UpdateMetaAsync(7, It.IsAny<FormMetaDto>()))
               .ReturnsAsync(dto);

            var controller = BuildController(app);
            var result = await controller.UpdateMeta(7, new FormMetaDto { Title = "new" });

            var ok = result as OkObjectResult;
            ok.Should().NotBeNull();
            ok!.Value.Should().Be(dto);
        }

        [Fact]
        public async Task AddLayout_Returns_Ok()
        {
            var app = new Mock<IFormAppService>();
            var dto = new FormOutDto { FormKey = 99 };
            app.Setup(a => a.AddLayoutAsync(99, It.IsAny<FormLayoutDto>()))
               .ReturnsAsync(dto);

            var controller = BuildController(app);
            var result = await controller.AddLayout(99, new FormLayoutDto { Sections = new() });

            var ok = result as OkObjectResult;
            ok.Should().NotBeNull();
            ok!.Value.Should().Be(dto);
        }

        [Fact]
        public async Task SetLayout_Returns_Ok()
        {
            var app = new Mock<IFormAppService>();
            var dto = new FormOutDto { FormKey = 5 };
            app.Setup(a => a.SetLayoutAsync(5, It.IsAny<FormLayoutDto>()))
               .ReturnsAsync(dto);

            var controller = BuildController(app);
            var result = await controller.SetLayout(5, new FormLayoutDto { Sections = new() });

            var ok = result as OkObjectResult;
            ok.Should().NotBeNull();
            ok!.Value.Should().Be(dto);
        }

        [Fact]
        public async Task SetStatus_BadRequest_When_BodyMissingOrEmpty()
        {
            var app = new Mock<IFormAppService>();
            var controller = BuildController(app);

            var bad1 = await controller.SetStatus(10, null!);
            bad1.Should().BeOfType<BadRequestObjectResult>();

            var bad2 = await controller.SetStatus(10, new StatusPatchDto { Status = "   " });
            bad2.Should().BeOfType<BadRequestObjectResult>();

            app.Verify(a => a.SetStatusAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task SetStatus_Ok_Calls_Service()
        {
            var app = new Mock<IFormAppService>();
            app.Setup(a => a.SetStatusAsync(10, "Published"))
               .ReturnsAsync(new FormOutDto { FormKey = 10, Status = "Published" });

            var controller = BuildController(app);
            var result = await controller.SetStatus(10, new StatusPatchDto { Status = "Published" });

            var ok = result as OkObjectResult;
            ok.Should().NotBeNull();
            var dto = ok!.Value as FormOutDto;
            dto!.Status.Should().Be("Published");
            app.Verify(a => a.SetStatusAsync(10, "Published"), Times.Once);
        }

        [Fact]
        public async Task SetAccess_BadRequest_When_BodyMissingOrEmpty()
        {
            var app = new Mock<IFormAppService>();
            var controller = BuildController(app);

            var bad1 = await controller.SetAccess(8, null!);
            bad1.Should().BeOfType<BadRequestObjectResult>();

            var bad2 = await controller.SetAccess(8, new AccessPatchDto { Access = "" });
            bad2.Should().BeOfType<BadRequestObjectResult>();

            app.Verify(a => a.SetAccessAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task SetAccess_Ok_Calls_Service()
        {
            var app = new Mock<IFormAppService>();
            app.Setup(a => a.SetAccessAsync(8, "Open"))
               .ReturnsAsync(new FormOutDto { FormKey = 8, Access = "Open" });

            var controller = BuildController(app);
            var result = await controller.SetAccess(8, new AccessPatchDto { Access = "Open" });

            var ok = result as OkObjectResult;
            ok.Should().NotBeNull();
            var dto = ok!.Value as FormOutDto;
            dto!.Access.Should().Be("Open");
            app.Verify(a => a.SetAccessAsync(8, "Open"), Times.Once);
        }

        [Fact]
        public async Task Delete_Returns_NoContent()
        {
            var app = new Mock<IFormAppService>();
            app.Setup(a => a.DeleteAsync(77)).Returns(Task.CompletedTask);

            var controller = BuildController(app);
            var result = await controller.Delete(77);

            result.Should().BeOfType<NoContentResult>();
            app.Verify(a => a.DeleteAsync(77), Times.Once);
        }

        [Fact]
        public async Task GetByKey_AdminPreview_PassesAllowPreviewTrue()
        {
            var app = new Mock<IFormAppService>();
            app.Setup(a => a.GetByKeyAsync(5, true, true))
               .ReturnsAsync(new FormOutDto { FormKey = 5, Status = "Draft" });

            var controller = BuildController(app, isAdmin: true);
            var result = await controller.GetByKey(5, mode: "preview");

            var ok = result as OkObjectResult;
            ok.Should().NotBeNull();
            var dto = ok!.Value as FormOutDto;
            dto!.FormKey.Should().Be(5);

            app.Verify(a => a.GetByKeyAsync(5, true, true), Times.Once);
        }

        [Fact]
        public async Task Assign_Unassign_ListAssignees_Success()
        {
            var app = new Mock<IFormAppService>();
            app.Setup(a => a.AssignUserAsync(10, 200L)).Returns(Task.CompletedTask);
            app.Setup(a => a.UnassignUserAsync(10, 200L)).Returns(Task.CompletedTask);
            app.Setup(a => a.ListAssigneesAsync(10))
               .ReturnsAsync(new[] { new { userId = 200L } }.Cast<object>());

            var controller = BuildController(app);

            (await controller.Assign(10, new AssignRequest { UserId = 200L }))
                .Should().BeOfType<NoContentResult>();

            (await controller.Unassign(10, 200L))
                .Should().BeOfType<NoContentResult>();

            var listRes = await controller.ListAssignees(10);
            var ok = listRes as OkObjectResult;
            ok.Should().NotBeNull();
            ok!.Value.Should().BeAssignableTo<IEnumerable<object>>();

            app.Verify(a => a.AssignUserAsync(10, 200L), Times.Once);
            app.Verify(a => a.UnassignUserAsync(10, 200L), Times.Once);
            app.Verify(a => a.ListAssigneesAsync(10), Times.Once);
        }
    }
}