using Microsoft.AspNetCore.Mvc;
using MTU.Services;

namespace MTU.Controllers
{
    public class ChatbotController : Controller
    {
        private readonly IGeminiService _geminiService;

        public ChatbotController(IGeminiService geminiService)
        {
            _geminiService = geminiService;
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
            {
                return BadRequest("Vui lòng nhập tin nhắn.");
            }

            var response = await _geminiService.GetResponseAsync(request.Message);
            return Json(new { success = true, response = response });
        }

        public class ChatRequest
        {
            public string? Message { get; set; }
        }
    }
}
