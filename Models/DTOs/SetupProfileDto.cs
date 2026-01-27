using System.ComponentModel.DataAnnotations;

namespace MTU.Models.DTOs
{
    public class SetupProfileDto
    {
        // Step 1: Basic Info (Required)
        [Required(ErrorMessage = "Vui lòng nhập họ")]
        [StringLength(50, ErrorMessage = "Họ không được vượt quá 50 ký tự")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên")]
        [StringLength(50, ErrorMessage = "Tên không được vượt quá 50 ký tự")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Gender { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(100)]
        public string? Location { get; set; }

        // Step 2: Academic Info (Optional)
        [StringLength(100)]
        public string? Faculty { get; set; }

        [StringLength(10)]
        public string? AcademicYear { get; set; }

        [StringLength(500)]
        public string? Interests { get; set; }

        // Step 3: Profile Media (Optional)
        [StringLength(500)]
        public string? Bio { get; set; }

        public IFormFile? AvatarFile { get; set; }

        public IFormFile? CoverImageFile { get; set; }
    }
}
