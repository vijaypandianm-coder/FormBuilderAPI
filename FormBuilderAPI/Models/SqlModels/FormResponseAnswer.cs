using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FormBuilderAPI.Models.SqlModels
{
    [Table("formresponseanswers")]
    public class FormResponseAnswer
    {
        [Key]
        public long Id { get; set; }

        [ForeignKey(nameof(FormResponse))]
        public long ResponseId { get; set; }

        [JsonIgnore]
        public FormResponse? FormResponse { get; set; }

        // denormalized for fast filtering
        public int? FormKey { get; set; }
        [Column(TypeName = "varchar(255)")]
        public string? FormId { get; set; }
        public long UserId { get; set; }

        [Required, Column(TypeName = "varchar(255)")]
        public string FieldId { get; set; } = default!;

        [Column(TypeName = "varchar(32)")]
        public string? FieldType { get; set; }

        public string? AnswerValue { get; set; }
        public DateTime SubmittedAt { get; set; }
    }
}