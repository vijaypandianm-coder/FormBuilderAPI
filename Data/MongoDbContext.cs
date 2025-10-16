using Microsoft.Extensions.Options;
using MongoDB.Driver;
using FormBuilderAPI.Models.MongoModels;

namespace FormBuilderAPI.Data
{
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } = default!;
        public string DatabaseName { get; set; } = default!;
    }

    public class MongoDbContext
    {
        private readonly IMongoDatabase _db;

        public MongoDbContext(IOptions<MongoDbSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            _db = client.GetDatabase(settings.Value.DatabaseName);
        }

        public IMongoCollection<Form> Forms => _db.GetCollection<Form>("forms");
        public IMongoCollection<Workflow> Workflows => _db.GetCollection<Workflow>("workflows");

        public IMongoCollection<Counter> Counters => _db.GetCollection<Counter>("counters");
    }
}