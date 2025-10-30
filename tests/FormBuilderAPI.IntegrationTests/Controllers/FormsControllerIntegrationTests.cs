using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using FormBuilderAPI.DTOs;
using FormBuilderAPI.IntegrationTests.TestFixtures;
using Xunit;

namespace FormBuilderAPI.IntegrationTests.Controllers
{
    public class FormsControllerIntegrationTests : IClassFixture<DatabaseFixture>
    {
        private readonly DatabaseFixture _fixture;
        private readonly ApiWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public FormsControllerIntegrationTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
            _factory = new ApiWebApplicationFactory(
                _fixture.MySqlConnectionString,
                _fixture.MongoConnectionString);
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GetForms_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/api/forms");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task CreateForm_WithValidData_ReturnsCreatedForm()
        {
            // Arrange
            var formDto = new CreateFormDto
            {
                Title = "Test Form",
                Description = "Test Description"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/forms", formDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdForm = await response.Content.ReadFromJsonAsync<FormDto>();
            createdForm.Should().NotBeNull();
            createdForm!.Title.Should().Be("Test Form");
        }
    }
}