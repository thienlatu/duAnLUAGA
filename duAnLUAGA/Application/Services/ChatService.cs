using duAnLUAGA.Application.Interfaces;
using System.Text;
using System.Text.Json;

namespace duAnLUAGA.Application.Services
{
    public class ChatService : IChatService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ChatService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> GetReplyAsync(string userMessage, string productContext)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            var modelName = _configuration["Gemini:ModelName"];
            var baseUrl = _configuration["Gemini:BaseUrl"];
            var apiUrl = $"{baseUrl}{modelName}:generateContent?key={apiKey}";

            // --- SỬA PROMPT ---
            string systemPrompt = $@"
Bạn là nhân viên bán sách của 'LUAGA Shop'.
DƯỚI ĐÂY LÀ KHO HÀNG (JSON):
{productContext}

NHIỆM VỤ:
Phân tích câu nói của khách: ""{userMessage}""

1. Nếu khách muốn tìm sách, mua hàng (vd: 'sách nào hay', 'truyện nguyễn nhật ánh'):
   - Tìm trong KHO HÀNG các sản phẩm phù hợp.
   - Trả về JSON: {{ ""type"": ""products"", ""reply"": ""Câu trả lời ngắn gọn"", ""products"": [Danh sách các object sản phẩm tìm được] }}
   - QUAN TRỌNG: Giữ nguyên tên trường (keys) trong JSON gốc: maSp, tenSanPham, gia, giaSauGiam, hinhDaiDien.

2. Nếu khách hỏi chuyện khác:
   - Trả về JSON: {{ ""type"": ""text"", ""reply"": ""Câu trả lời thân thiện"", ""products"": [] }}

YÊU CẦU: CHỈ TRẢ VỀ RAW JSON. Không được dùng markdown.";

            var payload = new
            {
                contents = new object[]
                {
                    new { role = "user", parts = new object[] { new { text = systemPrompt } } }
                }
            };

            try
            {
                string jsonPayload = JsonSerializer.Serialize(payload);
                HttpContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(apiUrl, content);
                string responseString = await response.Content.ReadAsStringAsync();

                using (JsonDocument doc = JsonDocument.Parse(responseString))
                {
                    if (doc.RootElement.TryGetProperty("candidates", out var candidates))
                    {
                        var text = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
                        // Clean markdown nếu có
                        text = text.Replace("```json", "").Replace("```", "").Trim();
                        return text;
                    }
                }
            }
            catch
            {
                return JsonSerializer.Serialize(new { type = "text", reply = "Lỗi kết nối AI.", products = new object[] { } });
            }

            return JsonSerializer.Serialize(new { type = "text", reply = "Bot chưa hiểu ý bạn.", products = new object[] { } });
        }
    }
}