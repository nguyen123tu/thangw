using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MTU.Models.Entities
{
    [Table("StoryReactions")]
    public class StoryReaction
    {
        [Key]
        public int StoryReactionId { get; set; }

        [Required]
        public int StoryId { get; set; }

        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// Loại reaction: heart, fire, laugh, wow, sad, clap
        /// </summary>
        [Required]
        [StringLength(20)]
        public string ReactionType { get; set; } = string.Empty;

        /// <summary>
        /// Emoji tương ứng để hiển thị
        /// </summary>
        [StringLength(10)]
        public string Emoji { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("StoryId")]
        public virtual Story? Story { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
