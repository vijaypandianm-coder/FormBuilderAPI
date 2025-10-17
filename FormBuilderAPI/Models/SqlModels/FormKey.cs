using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FormBuilderAPI.Models.SqlModels
{
    [Table("formkeys")]
    public class FormKey
    {
        [Key]
        [Column("FormKey")]
        public int Id { get; set; }            // AUTO_INCREMENT
        [Required, Column(TypeName = "varchar(24)")]
        public string FormId { get; set; } = default!;
    }
}