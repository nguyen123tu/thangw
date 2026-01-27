using Microsoft.AspNetCore.Http;

namespace MTU.Models.DTOs
{
    public class SendMessageDto
    {
        public int ReceiverId { get; set; }
        public string? Content { get; set; }
        public IFormFile? Image { get; set; }
    }
}
