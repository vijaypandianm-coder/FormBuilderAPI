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
        public MongoDbContext(IOptions<MongoDbSettings> options)
        {
            var client = new MongoClient(options.Value.ConnectionString);
            _db = client.GetDatabase(options.Value.DatabaseName);
        }

        public IMongoCollection<Form> Forms => _db.GetCollection<Form>("forms");
        public IMongoCollection<Workflow> Workflows => _db.GetCollection<Workflow>("workflows");
        // (Users are now in SQL, so no Users collection here)
    }
}
