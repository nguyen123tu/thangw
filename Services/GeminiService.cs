using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MTU.Services
{
    public interface IGeminiService
    {
        Task<string> GetResponseAsync(string prompt);
    }

    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _apiKey;
        private readonly string _apiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";


        public GeminiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Gemini:ApiKey"];
        }

        public async Task<string> GetResponseAsync(string prompt)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                return "Hệ thống chưa cấu hình API Key cho Gemini. Vui lòng liên hệ admin.";
            }

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = $@"Bạn là trợ lý AI ảo của mạng xã hội sinh viên MTU (MTU Social).
Phong cách: Thân thiện, trẻ trung (Gen Z), hữu ích, ngắn gọn, sử dụng emoji tự nhiên.
Nhiệm vụ: Giải đáp thắc mắc học tập, tư vấn tình cảm/bạn bè, gợi ý hoạt động ngoại khóa.
QUY TẮC TUYỆT ĐỐI: 
1. KHÔNG được sử dụng từ ngữ thô tục, chửi thề, tiếng lóng bậy bạ dưới mọi hình thức.
2. Nếu User chửi bậy, hãy nhắc nhở nhẹ nhàng và lịch sự từ chối trả lời hoặc chuyển chủ đề.
3. Không trả lời các vấn đề vi phạm đạo đức, chính trị nhạy cảm.
User hỏi: {prompt}" }
                        }
                    }
                }
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            try 
            {
                var response = await _httpClient.PostAsync($"{_apiUrl}?key={_apiKey}", jsonContent);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Gemini API Error: {response.StatusCode} - {errorContent}");
                    


                    return $"Xin lỗi, có lỗi xảy ra (Status: {response.StatusCode}). Chi tiết: {errorContent}";
                }

                var responseString = await response.Content.ReadAsStringAsync();
                var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseString);

                if (geminiResponse?.Candidates != null && geminiResponse.Candidates.Length > 0)
                {
                    var candidate = geminiResponse.Candidates[0];
                    if (candidate?.Content?.Parts != null && candidate.Content.Parts.Length > 0)
                    {
                        var text = candidate.Content.Parts[0].Text;
                        return text ?? "AI trả lời rỗng.";
                    }
                }

                return "AI không phản hồi nội dung nào.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"System Error: {ex}");
                return "Đã xảy ra lỗi hệ thống: " + ex.Message;
            }
        }

        // Helper classes for JSON deserialization
        private class GeminiResponse
        {
            [JsonPropertyName("candidates")]
            public Candidate[]? Candidates { get; set; }
        }

        private class Candidate
        {
            [JsonPropertyName("content")]
            public Content? Content { get; set; }
        }

        private class Content
        {
            [JsonPropertyName("parts")]
            public Part[]? Parts { get; set; }
        }

        private class Part
        {
            [JsonPropertyName("text")]
            public string? Text { get; set; }
        }
    }
}
