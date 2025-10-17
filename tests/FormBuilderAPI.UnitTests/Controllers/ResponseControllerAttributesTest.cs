using System.Linq;
using System.Reflection;
using FluentAssertions;
using FormBuilderAPI.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace FormBuilderAPI.UnitTests.Controllers
{
    public class ResponsesControllerAttributeTests
    {
        [Fact]
        public void Controller_Has_ApiController_And_Route()
        {
            var t = typeof(ResponsesController);
            t.GetCustomAttribute<ApiControllerAttribute>().Should().NotBeNull();

            var route = t.GetCustomAttribute<RouteAttribute>();
            route.Should().NotBeNull();
            route!.Template.Should().Be("api/[controller]");
        }

        [Fact]
        public void Submit_Has_Correct_Http_And_Policy()
        {
            var m = typeof(ResponsesController).GetMethod("Submit")!;
            var http = m.GetCustomAttributes<HttpPostAttribute>().FirstOrDefault();
            http.Should().NotBeNull();
            http!.Template.Should().Be("{formKey:int}");

            var auth = m.GetCustomAttribute<AuthorizeAttribute>();
            auth.Should().NotBeNull();
            auth!.Policy.Should().Be("RequireLearnerOrAdmin");
        }

        [Fact]
        public void List_Has_Correct_Http_And_Policy()
        {
            var m = typeof(ResponsesController).GetMethod("List")!;
            var http = m.GetCustomAttributes<HttpGetAttribute>().FirstOrDefault();
            http.Should().NotBeNull();
            http!.Template.Should().Be("{formKey:int}");

            var auth = m.GetCustomAttribute<AuthorizeAttribute>();
            auth.Should().NotBeNull();
            auth!.Policy.Should().Be("RequireLearnerOrAdmin");
        }
    }
}