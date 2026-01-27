namespace MTU.Models.DTOs
{
    public class StudentDto
    {
        public string MSSV { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;
        public string Faculty { get; set; } = string.Empty;
        public decimal GPA { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? PlaceOfBirth { get; set; }
        public string? Gender { get; set; }
    }
}
