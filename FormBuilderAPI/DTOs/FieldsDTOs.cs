// DTOs/FieldsDTOs.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FormBuilderAPI.DTOs
{
    public class FieldCreateDto
    {
        public string? FieldId { get; set; }

        [Required, StringLength(100, ErrorMessage = "Label max length is 100.")]
        public string  Label { get; set; } = default!;

        // Values like: shortText, longText, number, date, file, radio, dropdown, checkbox, multiselect
        [Required, StringLength(30)]
        public string  Type  { get; set; } = "text";

        public bool    IsRequired { get; set; }

        // For choice types only; left un-annotated since it’s conditional
        public List<string>? Options { get; set; }
    }

    public class FieldUpdateDto : FieldCreateDto {}
}