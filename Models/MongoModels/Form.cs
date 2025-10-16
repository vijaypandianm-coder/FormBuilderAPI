using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace FormBuilderAPI.Models.MongoModels
{
    [BsonIgnoreExtraElements]
    public class Form
    {
        // Map string <-> ObjectId and auto-generate on insert
        [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = default!;

        public int? FormKey { get; set; }
        public string Title { get; set; } = default!;
        public string? Description { get; set; }
        public string Status { get; set; } = "Draft";
        public string Access { get; set; } = "Open";
        public string CreatedBy { get; set; } = "system";
        public DateTime? PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public List<FormSection> Layout { get; set; } = new();
    }

    public class FormSection
    {
        public string SectionId { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string? Description { get; set; }
        public List<FormField> Fields { get; set; } = new();
    }

    public class FormField
    {
        public string FieldId { get; set; } = default!;
        public string Label { get; set; } = default!;
        public string Type { get; set; } = default!;
        public bool IsRequired { get; set; }
        public List<FieldOption>? Options { get; set; }   // for choice types only
    }

    public class FieldOption
    {
        public string Id { get; set; } = default!;  // stringified ObjectId GUIDs we generate
        public string Text { get; set; } = default!;
    }
}