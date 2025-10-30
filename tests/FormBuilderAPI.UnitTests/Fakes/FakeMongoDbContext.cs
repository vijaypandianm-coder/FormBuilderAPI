using FormBuilderAPI.Data;
using FormBuilderAPI.Models.MongoModels;
using MongoDB.Driver;
using Moq;

namespace FormBuilderAPI.UnitTests.Fakes
{
    /// <summary>
    /// A fake MongoDbContext you can inject into FormService
    /// so you can control/mock the collections.
    /// </summary>
    public class FakeMongoDbContext : MongoDbContext
    {
        private readonly Mock<IMongoCollection<Form>> _mockFormsCollection;

        public FakeMongoDbContext()
            : base(Microsoft.Extensions.Options.Options.Create(
                new MongoDbSettings { ConnectionString = "mongodb://localhost:27017", DatabaseName = "FakeDb" }))
        {
            _mockFormsCollection = new Mock<IMongoCollection<Form>>();
            Forms = _mockFormsCollection.Object;
        }

        public override IMongoCollection<Form> Forms { get; }
            = new Mock<IMongoCollection<Form>>().Object;
        public override IMongoCollection<Workflow> Workflows { get; }
            = new Mock<IMongoCollection<Workflow>>().Object;
        public override IMongoCollection<Counter> Counters { get; }
            = new Mock<IMongoCollection<Counter>>().Object;

        public void SetupFindAsync(Form form)
        {
            var cursor = new Mock<IAsyncCursor<Form>>();
            cursor.Setup(c => c.Current).Returns(new List<Form> { form });
            cursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockFormsCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<Form>>(),
                    It.IsAny<FindOptions<Form>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(cursor.Object);
        }

        public void SetupFindAsync(List<Form> forms)
        {
            var cursor = new Mock<IAsyncCursor<Form>>();
            cursor.Setup(c => c.Current).Returns(forms);
            cursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockFormsCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<Form>>(),
                    It.IsAny<FindOptions<Form>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(cursor.Object);
        }
    }
}