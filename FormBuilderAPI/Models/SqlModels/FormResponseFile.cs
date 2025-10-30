using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FormBuilderAPI.Models.SqlModels
{
    [Table("formresponsefiles")]
    public class FormResponseFile
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long ResponseId { get; set; }

        [Required]
        public int FormKey { get; set; }

        [Required, Column(TypeName = "varchar(255)")]
        public string FieldId { get; set; } = default!;

        [Required, Column(TypeName = "varchar(255)")]
        public string FileName { get; set; } = default!;

        [Required, Column(TypeName = "varchar(100)")]
        public string ContentType { get; set; } = "application/octet-stream";

        [Required]
        public long SizeBytes { get; set; }

        [Required]
        public byte[] Blob { get; set; } = Array.Empty<byte>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}