using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FormBuilderAPI.Models.SqlModels
{
    [Table("formresponses")]
    public class FormResponse
    {
        [Key]
        public long Id { get; set; }  // BIGINT AUTO_INCREMENT

        // Mongo Form _id (string)
        [Required]
        public string FormId { get; set; } = default!;

        [Required]
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        // FK -> users.Id
        [ForeignKey(nameof(User))]
        public long UserId { get; set; }

        public User? User { get; set; }

        public List<FormResponseAnswer> Answers { get; set; } = new();
    }
}