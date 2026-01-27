using System.ComponentModel.DataAnnotations;

namespace MTU.Models.DTOs
{
    public class CreateCommentDto
    {
        [Required(ErrorMessage = "Post ID is required")]
        public int PostId { get; set; }

        [Required(ErrorMessage = "Comment content is required")]
        [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters")]
        public string Content { get; set; } = string.Empty;
    }
}
