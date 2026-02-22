using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MTU.Models.Entities
{
    [Table("StoryViews")]
    public class StoryView
    {
        [Key]
        public int StoryViewId { get; set; }

        [Required]
        public int StoryId { get; set; }

        [Required]
        public int ViewerId { get; set; }

        public DateTime ViewedAt { get; set; } = DateTime.Now;

        [ForeignKey("StoryId")]
        public virtual Story? Story { get; set; }

        [ForeignKey("ViewerId")]
        public virtual User? Viewer { get; set; }
    }
}
