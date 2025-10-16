using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FormBuilderAPI.Models.SqlModels
{
    [Table("users")]
    public class User
    {
        // BIGINT AUTO_INCREMENT PK
        [Key]
        public long Id { get; set; }

        // Map to existing column name "UserName" (you created it earlier)
        [Required]
        [Column("UserName")]
        public string Username { get; set; } = default!;

        [Required, EmailAddress]
        public string Email { get; set; } = default!;

        [Required]
        public string PasswordHash { get; set; } = default!;

        // These two are used by your AuthService & policies
        [Required]
        public string Role { get; set; } = "Learner";
        //public string? DisplayName { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
