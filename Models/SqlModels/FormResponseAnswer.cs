using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FormBuilderAPI.Models.SqlModels
{
    [Table("formresponseanswers")]
    public class FormResponseAnswer
    {
        [Key]
        public long Id { get; set; }  // BIGINT AUTO_INCREMENT

        // FK -> formresponses.Id
        [ForeignKey(nameof(FormResponse))]
        public long ResponseId { get; set; }
        
        [JsonIgnore]
        public FormResponse? FormResponse { get; set; }

        // Field's identifier from the Mongo layout (e.g., "fullName", "q1", etc.)
        [Required]
        public string FieldId { get; set; } = default!;

        // Free-form answer (TEXT/LONGTEXT)
        public string? AnswerValue { get; set; }
    }
}
