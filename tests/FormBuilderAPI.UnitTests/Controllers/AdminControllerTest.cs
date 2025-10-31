using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using FormBuilderAPI.Controllers;
using FormBuilderAPI.Application.Interfaces;

public class AdminControllerTests
{
    [Fact]
    public async Task ListForms_Returns_Ok_With_Paging_And_Total()
    {
        var app = new Mock<IFormAppService>();
        var items = new List<object> { new { Id = 1 }, new { Id = 2 } };
        app.Setup(a => a.ListAsync("Published", true, 2, 10))
           .ReturnsAsync((items, 99L));

        var ctrl = new AdminController(app.Object);
        var res = await ctrl.ListForms("Published", 2, 10);

        var ok = res as OkObjectResult;
        ok.Should().NotBeNull();
        dynamic body = ok!.Value!;
        ((long)body.total).Should().Be(99);
        ((int)body.page).Should().Be(2);
        ((int)body.pageSize).Should().Be(10);
    }
}