namespace FormBuilderAPI.DTOs
{
    /// <summary>Used to create a new form (meta only).</summary>
    public class FormCreateDto
    {
        public string Title { get; set; } = default!;
        public string? Description { get; set; }
    }
}