using MongoDB.Bson.Serialization.Attributes;

namespace FormBuilderAPI.Models.MongoModels
{
    public class FormSection
    {
        [BsonElement("sectionId")]
        public string SectionId { get; set; } = Guid.NewGuid().ToString();  // ← stable id

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
        public string FieldId { get; set; } = Guid.NewGuid().ToString();     // ← stable id

        [BsonElement("label")]
        public string Label { get; set; } = string.Empty;

        [BsonElement("type")]
        public string Type { get; set; } = "text"; // shortText,longText,number,dropdown,date,file

        [BsonElement("isRequired")]
        public bool IsRequired { get; set; } = false;

        [BsonElement("options")]
        public List<string>? Options { get; set; }
    }
}