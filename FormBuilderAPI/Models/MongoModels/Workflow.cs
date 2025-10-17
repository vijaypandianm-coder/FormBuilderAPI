using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FormBuilderAPI.Models.MongoModels
{
    public class Workflow
    {
        [BsonId, BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = default!;
        [BsonRepresentation(BsonType.ObjectId)]
        public string FormId { get; set; } = default!;
        public int UsageCount { get; set; } = 0;
        public string Name { get; set; } = "Default Workflow";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}