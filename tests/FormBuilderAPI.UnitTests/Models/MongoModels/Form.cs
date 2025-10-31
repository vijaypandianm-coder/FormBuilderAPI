using System;
using System.Collections.Generic;

namespace FormBuilderAPI.Models.MongoModels
{
    public class Form
    {
        public string Id { get; set; } = string.Empty;
        public int? FormKey { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Access { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? PublishedAt { get; set; }
        public List<FormSection>? Layout { get; set; }
    }

    public class FormSection
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public List<FormField>? Fields { get; set; }
    }

    public class FormField
    {
        public string FieldId { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public List<FieldOption>? Options { get; set; }
    }

    public class FieldOption
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    public class Counter
    {
        public string Id { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
