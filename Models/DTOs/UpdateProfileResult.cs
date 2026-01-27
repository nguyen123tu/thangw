namespace MTU.Models.DTOs
{
    public class UpdateProfileResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? UpdatedValue { get; set; }
    }
}
