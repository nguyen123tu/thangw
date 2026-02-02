using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MTU.Models.Entities
{
    [Table("Posts")]
    public class Post
    {
        [Key]
        public int PostId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? Content { get; set; }

        [StringLength(255)]
        public string? ImageUrl { get; set; }

        [StringLength(255)]
        public string? VideoUrl { get; set; }

        [StringLength(255)]
        public string? FileUrl { get; set; }

        [StringLength(255)]
        public string? FileName { get; set; }

        [StringLength(100)]
        public string? Location { get; set; }

        [Required]
        [StringLength(20)]
        public string Privacy { get; set; } = "public";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<Like> Likes { get; set; } = new List<Like>();
        public virtual ICollection<SavedPost> SavedPosts { get; set; } = new List<SavedPost>();
        public virtual ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
    }
}
