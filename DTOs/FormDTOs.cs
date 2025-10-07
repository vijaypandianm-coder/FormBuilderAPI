using FormBuilderAPI.Models.MongoModels;

namespace FormBuilderAPI.DTOs
{
    public class CreateFormRequest
    {
        public string Title { get; set; } = string.Empty;
        public List<FormSection> Layout { get; set; } = new();
    }

    public class UpdateFormRequest
    {
        public string Title { get; set; } = string.Empty;
        public List<FormSection> Layout { get; set; } = new();
    }
}