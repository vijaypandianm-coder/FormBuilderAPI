using System.Linq;
using FluentAssertions;
using FormBuilderAPI.Application.Interfaces;
using FormBuilderAPI.Controllers;
using FormBuilderAPI.DTOs;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace FormBuilderAPI.UnitTests.Controllers
{
    public class AdminControllerTests
    {
        [Fact]
        public async Task ListForms_Returns_Ok_With_Paging_Shape()
        {
            // Arrange
            var app = new Mock<IFormAppService>();
            var items = new[]
            {
                new FormOutDto { FormKey = 1, Title = "A" },
                new FormOutDto { FormKey = 2, Title = "B" }
            }.AsEnumerable();

            app.Setup(a => a.ListAsync("Draft", true, 2, 5))
               .ReturnsAsync((items, 10L));

            var controller = new AdminController(app.Object);

            // Act
            var result = await controller.ListForms(status: "Draft", page: 2, pageSize: 5);

            // Assert
            var ok = result as OkObjectResult;
            ok.Should().NotBeNull();

            var value = ok!.Value!;
            var totalProp = value.GetType().GetProperty("total")!;
            var pageProp = value.GetType().GetProperty("page")!;
            var pageSizeProp = value.GetType().GetProperty("pageSize")!;
            var itemsProp = value.GetType().GetProperty("items")!;

            totalProp.GetValue(value).Should().Be(10L);
            pageProp.GetValue(value).Should().Be(2);
            pageSizeProp.GetValue(value).Should().Be(5);

            var returnedItems = (itemsProp.GetValue(value) as System.Collections.IEnumerable)!;
            returnedItems.Cast<FormOutDto>().Select(x => x.FormKey).Should().BeEquivalentTo(new[] { 1, 2 });

            app.Verify(a => a.ListAsync("Draft", true, 2, 5), Times.Once);
        }
    }
}