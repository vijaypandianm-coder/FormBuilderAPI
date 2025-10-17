// DTOs/ResponsesDTOs.cs
using System.Collections.Generic;

namespace FormBuilderAPI.DTOs
{
    public class SubmitAnswerDto
    {
        public string FieldId { get; set; } = default!;

        // non-choice types use this
        public string? AnswerValue { get; set; }

        // choice types (radio/checkbox/dropdown/multiselect) send only option ids
        public List<string>? OptionIds { get; set; }
    }

    public class SubmitResponseDto
    {
        public List<SubmitAnswerDto> Answers { get; set; } = new();
    }
}