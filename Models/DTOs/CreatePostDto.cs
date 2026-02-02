using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MTU.Models.DTOs
{
    public class CreatePostDto
    {
        [StringLength(5000, ErrorMessage = "Post content cannot exceed 5000 characters")]
        public string? Content { get; set; }

        public IFormFile? Image { get; set; }

        public IFormFile? Video { get; set; }

        public IFormFile? Attachment { get; set; }

        [StringLength(20)]
        public string? Privacy { get; set; } = "public";
    }
}
