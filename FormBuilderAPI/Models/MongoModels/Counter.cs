// Models/MongoModels/Counter.cs
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FormBuilderAPI.Models.MongoModels
{
    public class Counter
    {
        [BsonId]
        public string Id { get; set; } = default!;   // e.g. "FormKey"
        [BsonElement("value")]
        public int Value { get; set; }
    }
}