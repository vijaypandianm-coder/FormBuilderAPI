using MongoDB.Driver;
using FormBuilderAPI.Models.MongoModels;

namespace FormBuilderAPI.Data
{
    public interface IMongoDbContext
    {
        IMongoCollection<Form> Forms { get; }
        IMongoCollection<Counter> Counters { get; }
    }
}