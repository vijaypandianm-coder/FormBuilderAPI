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
        public FakeMongoDbContext()
            : base(Microsoft.Extensions.Options.Options.Create(
                new MongoDbSettings { ConnectionString = "mongodb://localhost:27017", DatabaseName = "FakeDb" }))
        {
        }

        public override IMongoCollection<Form> Forms { get; }
        public override IMongoCollection<Workflow> Workflows { get; }
        public override IMongoCollection<Counter> Counters { get; }

        public FakeMongoDbContext(
            IMongoCollection<Form>? forms = null,
            IMongoCollection<Workflow>? workflows = null,
            IMongoCollection<Counter>? counters = null)
            : this()
        {
            Forms = forms ?? new Mock<IMongoCollection<Form>>().Object;
            Workflows = workflows ?? new Mock<IMongoCollection<Workflow>>().Object;
            Counters = counters ?? new Mock<IMongoCollection<Counter>>().Object;
        }
    }
}