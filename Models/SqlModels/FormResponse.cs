using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FormBuilderAPI.Models.SqlModels
{
    [Table("formresponses")]
    public class FormResponse
    {
        [Key]
        public long Id { get; set; }                 // BIGINT

        [Required]
        public string FormId { get; set; } = default!; // Mongo _id

        [Required]
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(User))]
        public long UserId { get; set; }            // FK -> users.Id

        public User? User { get; set; }             // nav

        public List<FormResponseAnswer> Answers { get; set; } = new();
    }
}