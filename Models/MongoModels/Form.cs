using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FormBuilderAPI.Models.MongoModels
{
    public class Form
    {
        [BsonId] 
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;

        // Layout remains the same
        [BsonElement("layout")]
        public List<FormSection> Layout { get; set; } = new();

        // New publishing fields
        [BsonElement("status")]
        public string Status { get; set; } = "Draft"; // Draft | Published

        [BsonElement("createdBy")]
        public string CreatedBy { get; set; } = "system";

        [BsonElement("publishedAt")]
        public DateTime? PublishedAt { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }

    public class FormSection
    {
        [BsonElement("sectionId")]
        public string SectionId { get; set; } = Guid.NewGuid().ToString();

        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;

        [BsonElement("description")]
        public string? Description { get; set; }

        [BsonElement("fields")]
        public List<FormField> Fields { get; set; } = new();
    }

    public class FormField
    {
        [BsonElement("fieldId")]
        public string FieldId { get; set; } = Guid.NewGuid().ToString();

        [BsonElement("label")]
        public string Label { get; set; } = string.Empty;

        // shortText,longText,number,dropdown,date,file
        [BsonElement("type")]
        public string Type { get; set; } = "shortText";

        [BsonElement("isRequired")]
        public bool IsRequired { get; set; }

        [BsonElement("options")]
        public List<string>? Options { get; set; }
    }
}
