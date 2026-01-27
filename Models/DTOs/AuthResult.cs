using MTU.Models.Entities;

namespace MTU.Models.DTOs
{
    public class AuthResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public User? User { get; set; }
    }
}
