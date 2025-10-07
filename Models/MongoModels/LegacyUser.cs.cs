using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FormBuilderAPI.Models.MongoModels
{
    public class LegacyUser
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("username")]
        public string Username { get; set; } = string.Empty;

        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("passwordHash")]
        public string PasswordHash { get; set; } = string.Empty;

        [BsonElement("role")]
        public string Role { get; set; } = "Learner";   // ✅ single role, not Roles[]

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;      // ✅ added for AuthService checks

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

