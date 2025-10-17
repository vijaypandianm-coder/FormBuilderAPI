using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

using FormBuilderAPI.Controllers;

namespace FormBuilderAPI.UnitTests.Controllers
{
    public class ControllerAttributeTests
    {
        // ---------- AuthController ----------
        [Fact]
        public void AuthController_Should_Have_ApiController_And_Route()
        {
            var t = typeof(AuthController);

            t.GetCustomAttributes(typeof(ApiControllerAttribute), inherit: true)
             .Should().NotBeEmpty("AuthController must be ApiController");

            var route = t.GetCustomAttributes(typeof(RouteAttribute), true)
                         .Cast<RouteAttribute>()
                         .Single();

            route.Template.Should().Be("api/[controller]");
        }

        [Fact]
        public void AuthController_Register_Should_Have_HttpPost_Register()
        {
            var m = typeof(AuthController).GetMethod("Register");
            m.Should().NotBeNull();

            var post = m!.GetCustomAttributes(typeof(HttpPostAttribute), true)
                         .Cast<HttpPostAttribute>()
                         .Single();

            post.Template.Should().Be("register");
        }

        [Fact]
        public void AuthController_Login_Should_Have_HttpPost_Login()
        {
            var m = typeof(AuthController).GetMethod("Login");
            m.Should().NotBeNull();

            var post = m!.GetCustomAttributes(typeof(HttpPostAttribute), true)
                         .Cast<HttpPostAttribute>()
                         .Single();

            post.Template.Should().Be("login");
        }

        // ---------- ResponsesController ----------
        [Fact]
        public void ResponsesController_Should_Have_ApiController_And_Route()
        {
            var t = typeof(ResponsesController);

            t.GetCustomAttributes(typeof(ApiControllerAttribute), true)
             .Should().NotBeEmpty();

            var route = t.GetCustomAttributes(typeof(RouteAttribute), true)
                         .Cast<RouteAttribute>()
                         .Single();

            route.Template.Should().Be("api/[controller]");
        }

        [Fact]
        public void ResponsesController_Submit_Should_Have_HttpPost_And_Authorize_With_Policy()
        {
            var m = typeof(ResponsesController).GetMethod("Submit");
            m.Should().NotBeNull();

            var post = m!.GetCustomAttributes(typeof(HttpPostAttribute), true)
                         .Cast<HttpPostAttribute>()
                         .Single();
            post.Template.Should().Be("{formKey:int}");

            var auth = m.GetCustomAttributes(typeof(AuthorizeAttribute), true)
                        .Cast<AuthorizeAttribute>()
                        .Single();

            auth.Policy.Should().Be("RequireLearnerOrAdmin");
        }

        [Fact]
        public void ResponsesController_List_Should_Have_HttpGet_And_Authorize_With_Policy()
        {
            var m = typeof(ResponsesController).GetMethod("List");
            m.Should().NotBeNull();

            var get = m!.GetCustomAttributes(typeof(HttpGetAttribute), true)
                        .Cast<HttpGetAttribute>()
                        .Single();
            get.Template.Should().Be("{formKey:int}");

            var auth = m.GetCustomAttributes(typeof(AuthorizeAttribute), true)
                        .Cast<AuthorizeAttribute>()
                        .Single();

            auth.Policy.Should().Be("RequireLearnerOrAdmin");
        }
    }
}