using System.ComponentModel.DataAnnotations;

namespace MTU.Models.DTOs
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Email trường là bắt buộc")]
        [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@mtu\.edu\.vn$", ErrorMessage = "Email phải thuộc tên miền @mtu.edu.vn")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
        public string Email { get; set; } = string.Empty;
    }
}
