using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using FormBuilderAPI.Controllers;
using FormBuilderAPI.Application.Interfaces;
using FormBuilderAPI.DTOs;
using FormBuilderAPI.UnitTests.TestUtils;

public class FormsControllerTests : ControllerTestBase
{
    private static FormsController MakeCtrl(Mock<IFormAppService> app, bool isAdmin, string? userId="123")
    {
        var ctrl = new FormsController(app.Object);
        SetUser(ctrl, isAdmin, userId);
        return ctrl;
    }

    [Fact]
    public async Task CreateMeta_Admin_CreatedAtAction()
    {
        var app = new Mock<IFormAppService>();
        app.Setup(a => a.CreateMetaAsync(It.IsAny<string>(), It.IsAny<FormMetaDto>()))
           .ReturnsAsync(new FormOutDto{ FormKey = 77 });

        var ctrl = MakeCtrl(app, true, "999");
        var result = await ctrl.CreateMeta(new FormMetaDto { FormId = "abc" });

        result.Should().BeOfType<CreatedAtActionResult>();
        var created = (CreatedAtActionResult)result;
        ((FormOutDto)created.Value!).FormKey.Should().Be(77);
    }

    [Fact]
    public async Task UpdateMeta_Admin_Ok()
    {
        var app = new Mock<IFormAppService>();
        app.Setup(a => a.UpdateMetaAsync(11, It.IsAny<FormMetaDto>()))
           .ReturnsAsync(new FormOutDto{ FormKey = 11 });

        var ctrl = MakeCtrl(app, true);
        var ok = await ctrl.UpdateMeta(11, new FormMetaDto { Title="x" }) as OkObjectResult;

        ((FormOutDto)ok!.Value!).FormKey.Should().Be(11);
    }

    [Fact]
    public async Task AddLayout_And_SetLayout_Admin_Ok()
    {
        var app = new Mock<IFormAppService>();
        app.Setup(a => a.AddLayoutAsync(5, It.IsAny<FormLayoutDto>()))
           .ReturnsAsync(new FormOutDto{ FormKey = 5 });
        app.Setup(a => a.SetLayoutAsync(5, It.IsAny<FormLayoutDto>()))
           .ReturnsAsync(new FormOutDto{ FormKey = 5 });

        var ctrl = MakeCtrl(app, true);
        (await ctrl.AddLayout(5, new FormLayoutDto())).Should().BeOfType<OkObjectResult>();
        (await ctrl.SetLayout(5, new FormLayoutDto())).Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SetStatus_BadRequest_When_Missing()
    {
        var app = new Mock<IFormAppService>();
        var ctrl = MakeCtrl(app, true);

        var res = await ctrl.SetStatus(1, new StatusPatchDto{ Status = "  " });
        res.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SetAccess_BadRequest_When_Missing()
    {
        var app = new Mock<IFormAppService>();
        var ctrl = MakeCtrl(app, true);

        var res = await ctrl.SetAccess(1, new AccessPatchDto{ Access = "" });
        res.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Delete_Admin_NoContent()
    {
        var app = new Mock<IFormAppService>();
        app.Setup(a => a.DeleteAsync(9)).Returns(Task.CompletedTask);

        var ctrl = MakeCtrl(app, true);
        var res = await ctrl.Delete(9);

        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetByKey_Admin_Allows_Preview()
    {
        var app = new Mock<IFormAppService>();
        app.Setup(a => a.GetByKeyAsync(3, true, true))
           .ReturnsAsync(new FormOutDto{ FormKey = 3 });

        var ctrl = MakeCtrl(app, true);
        var ok = await ctrl.GetByKey(3, "preview") as OkObjectResult;

        ((FormOutDto)ok!.Value!).FormKey.Should().Be(3);
    }

    [Fact]
    public async Task List_Admin_Returns_Raw_Items_And_Total()
    {
        var app = new Mock<IFormAppService>();
        var items = new List<FormOutDto> { new(){ FormKey = 1 } };
        app.Setup(a => a.ListAsync(null, true, 1, 20))
           .ReturnsAsync((Items: items.AsEnumerable(), Total: 1L));

        var ctrl = MakeCtrl(app, true);
        var ok = await ctrl.List(null, 1, 20) as OkObjectResult;

        dynamic body = ok!.Value!;
        ((long)body.Total).Should().Be(1);
        ((int)body.Page).Should().Be(1);
        ((int)body.PageSize).Should().Be(20);
        ((IEnumerable<FormOutDto>)body.Items).Should().HaveCount(1);
    }

    [Fact]
    public async Task List_Learner_Filters_Published_And_Assignments()
    {
        var app = new Mock<IFormAppService>();

        var forms = new List<FormOutDto>{
            new(){ FormKey=10, Status="Draft", Access="Open" },               // excluded (not published)
            new(){ FormKey=11, Status="Published", Access="Open" },           // included
            new(){ FormKey=12, Status="Published", Access="Restricted" }      // included only if assigned
        };

        app.Setup(a => a.ListAsync(null, false, 1, 20))
           .ReturnsAsync((forms.AsEnumerable(), 3L));

        app.Setup(a => a.ListAssigneesAsync(12))
           .ReturnsAsync(new List<long> { 123, 999 }.Cast<object>());

        var ctrl = MakeCtrl(app, isAdmin:false, userId:"123");

        var ok = await ctrl.List(null, 1, 20) as OkObjectResult;
        dynamic body = ok!.Value!;
        var items = ((IEnumerable<FormOutDto>)body.Items).ToList();

        items.Should().HaveCount(2);
        items.Select(i => i.FormKey).Should().BeEquivalentTo(new[]{ 11, 12 });
        ((long)body.Total).Should().Be(2);
    }

    [Fact]
    public async Task List_Learner_Without_UserId_Returns_Unauthorized()
    {
        var app = new Mock<IFormAppService>();
        var ctrl = MakeCtrl(app, isAdmin:false, userId:null);

        var res = await ctrl.List(null, 1, 20);
        res.Should().BeOfType<UnauthorizedResult>();
    }
}