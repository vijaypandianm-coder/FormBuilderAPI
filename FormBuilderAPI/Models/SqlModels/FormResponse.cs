using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FormBuilderAPI.Models.SqlModels
{
    [Table("formresponses")]
    public class FormResponse
    {
        [Key]
        public long Id { get; set; }

        // Mongo Form Id (stringified ObjectId)
        [Required, Column(TypeName = "varchar(24)")]
        public string FormId { get; set; } = default!;

        // Numeric key used in routes
        [Required]
        public int FormKey { get; set; }

        // Who submitted
        [Required]
        public long UserId { get; set; }

        // === per-answer columns (these are the ones your service writes) ===
        

        // For choice: optionId (single) or JSON array string (multi)
        // For non-choice: the typed value

        public DateTime SubmittedAt { get; set; }
    }
}