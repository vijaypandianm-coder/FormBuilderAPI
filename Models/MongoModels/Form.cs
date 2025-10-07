using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FormBuilderAPI.Models.MongoModels
{
    public class Form
    {
        [BsonId] [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;

        [BsonElement("layout")]
        public List<FormSection> Layout { get; set; } = new();

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }
}