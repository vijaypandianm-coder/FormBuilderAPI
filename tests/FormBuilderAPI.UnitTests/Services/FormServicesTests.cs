using System.Threading.Tasks;
using Xunit;
using Moq;
using MongoDB.Driver;
using FormBuilderAPI.Services;
using FormBuilderAPI.Models.MongoModels;
using FormBuilderAPI.UnitTests.Fakes;
using FormBuilderAPI.UnitTests.Helpers;
using System.Collections.Generic;

namespace FormBuilderAPI.UnitTests.Services
{
    public class FormServiceTests
    {
        private readonly Mock<IMongoCollection<Form>> _formsMock;
        private readonly FormService _service;

        public FormServiceTests()
        {
            _formsMock = new Mock<IMongoCollection<Form>>();
            var fakeContext = new FakeMongoDbContext(forms: _formsMock.Object);
            _service = new FormService(fakeContext);
        }

        [Fact]
        public async Task CreateFormAsync_sets_defaults_and_inserts()
        {
            var form = TestDataFactory.CreateSampleForm(status: null);

            await _service.CreateFormAsync(form);

            Assert.Equal("Draft", form.Status);
            Assert.Equal("Open", form.Access);
            _formsMock.Verify(f => f.InsertOneAsync(
                It.IsAny<Form>(),
                null,
                default), Times.Once);
        }

        [Fact]
        public async Task GetByFormKeyAsync_returns_form()
        {
            var form = TestDataFactory.CreateSampleForm();
            var cursor = new Mock<IAsyncCursor<Form>>();
            cursor.SetupSequence(c => c.MoveNextAsync(default))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            cursor.SetupGet(c => c.Current).Returns(new List<Form> { form });

            _formsMock.Setup(c =>
                c.FindAsync(It.IsAny<FilterDefinition<Form>>(),
                    It.IsAny<FindOptions<Form, Form>>(),
                    default))
                .ReturnsAsync(cursor.Object);

            var result = await _service.GetByFormKeyAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result!.FormKey);
        }
    }
}