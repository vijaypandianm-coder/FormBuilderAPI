// DTOs/FormDTOs.cs
using System;
using System.Collections.Generic;

namespace FormBuilderAPI.DTOs
{
    // ----- OUT -----
    public class FormOutDto
    {
        public string Id { get; set; } = default!;    // mongo _id
        public int? FormKey { get; set; }             // numeric key (if used)
        public string Title { get; set; } = default!;
        public string? Description { get; set; }
        public string Status { get; set; } = "Draft";
        public string Access { get; set; } = "Open";
        public string CreatedBy { get; set; } = "system";
        public DateTime? PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public List<FormSectionDto>? Layout { get; set; } // null in list, included in get-by-key
    }

    public class FormSectionDto
    {
        public string? SectionId { get; set; }
        public string Title { get; set; } = default!;
        public string? Description { get; set; }
        public List<FormFieldDto> Fields { get; set; } = new();
    }

    public class FormFieldDto
    {
        public string? FieldId { get; set; }
        public string Label { get; set; } = default!;
        public string Type { get; set; } = "text"; // text,longText,number,date,file,radio,dropdown,checkbox,multiselect
        public bool IsRequired { get; set; }

        // OUTPUT ONLY: normalized options with ids (only for choice types)
        public List<FieldOptionDto>? Options { get; set; }
    }

    public class FieldOptionDto
    {
        public string Id { get; set; } = default!;
        public string Text { get; set; } = default!;
    }

    // ----- IN -----
    public class FormMetaDto
    {
        public string Title { get; set; } = default!;
        public string? Description { get; set; }
    }

    // Append or replace layout (sections + fields)
    public class FormLayoutDto
    {
        public List<FormSectionCreateDto> Sections { get; set; } = new();
    }

    public class FormSectionCreateDto
    {
        public string? SectionId { get; set; }
        public string Title { get; set; } = default!;
        public string? Description { get; set; }
        public List<FieldCreateDto> Fields { get; set; } = new();
    }

    // For “single-field” UIs (optional helper)
    public class SingleFieldDto
    {
        public string? FieldId { get; set; }        // empty/new => create
        public string Label { get; set; } = default!;
        public string Type { get; set; } = "text";
        public bool IsRequired { get; set; }
        public List<string>? Options { get; set; }  // only if choice type
    }
}