using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MTU.Models.Entities
{
    [Table("Stories")]
    public class Story
    {
        [Key]
        public int StoryId { get; set; }

        [Required]
        public int UserId { get; set; }

        [StringLength(500)]
        public string? MediaUrl { get; set; }

        [StringLength(20)]
        public string MediaType { get; set; } = "image"; // "image" or "video"

        [StringLength(500)]
        public string? Caption { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime ExpiresAt { get; set; } = DateTime.Now.AddHours(24);

        public int ViewCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        // Navigation property
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
