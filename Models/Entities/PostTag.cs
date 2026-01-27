using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MTU.Models.Entities
{
    [Table("PostTags")]
    public class PostTag
    {
        [Key]
        public int PostTagId { get; set; }

        [Required]
        public int PostId { get; set; }

        [Required]
        public int TagId { get; set; }

        // Navigation properties
        [ForeignKey("PostId")]
        public virtual Post Post { get; set; } = null!;

        [ForeignKey("TagId")]
        public virtual Tag Tag { get; set; } = null!;
    }
}
