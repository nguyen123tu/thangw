using System.ComponentModel.DataAnnotations;

namespace MTU.Models.DTOs
{
    public class UpdateProfileDto
    {
        [StringLength(50)]
        public string? FirstName { get; set; }

        [StringLength(50)]
        public string? LastName { get; set; }

        [StringLength(500, ErrorMessage = "Giới thiệu không được vượt quá 500 ký tự")]
        public string? Bio { get; set; }

        [StringLength(100, ErrorMessage = "Nơi sinh sống không được vượt quá 100 ký tự")]
        public string? Location { get; set; }

        [StringLength(100, ErrorMessage = "Nơi sinh không được vượt quá 100 ký tự")]
        public string? PlaceOfBirth { get; set; }

        [StringLength(50)]
        public string? Gender { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(200, ErrorMessage = "Sở thích không được vượt quá 200 ký tự")]
        public string? Interests { get; set; }

        [StringLength(100, ErrorMessage = "Ngành học không được vượt quá 100 ký tự")]
        public string? Faculty { get; set; }

        [StringLength(10, ErrorMessage = "Niên khóa không được vượt quá 10 ký tự")]
        public string? AcademicYear { get; set; }

        public IFormFile? AvatarFile { get; set; }

        public IFormFile? CoverImageFile { get; set; }
    }
}
