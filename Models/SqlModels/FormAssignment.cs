using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FormBuilderAPI.Models.SqlModels
{
    [Table("formassignments")]
    public class FormAssignment
    {
        [Key]
        public long Id { get; set; }

        // Mongo form id
        [Required]
        [Column(TypeName = "varchar(24)")]
        public string FormId { get; set; } = default!;

        // FK -> users.Id
        [Required]
        public long UserId { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        // Optional: who assigned (admin id)
        public long? AssignedBy { get; set; }
        public int? SequenceNo { get; set; }
    }
}