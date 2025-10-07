namespace FormBuilderAPI.DTOs
{
    public class SubmitResponseRequest
    {
        // optional: client can echo section for analytics; not required
        public List<AnswerDTO> Answers { get; set; } = new();
    }

    public class AnswerDTO
    {
        public string FieldId { get; set; } = default!;      // ← must match FormField.FieldId
        public string? SectionId { get; set; }               // ← optional, matches FormSection.SectionId
        public string AnswerText { get; set; } = default!;   // store as text; files handled by upload svc
    }
}