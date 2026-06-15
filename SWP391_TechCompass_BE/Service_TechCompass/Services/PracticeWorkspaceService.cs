using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs;
using Service_TechCompass.DTOs.Assessment;
using Service_TechCompass.DTOs.Practice;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class PracticeWorkspaceService : IPracticeWorkspaceService
    {
        private readonly IPracticeWorkspaceRepository _repository;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public PracticeWorkspaceService(IPracticeWorkspaceRepository repository, HttpClient httpClient, IConfiguration configuration)
        {
            _repository = repository;
            _httpClient = httpClient;
            _configuration = configuration;
        }

        // Chức năng 35: Run Code (Sử dụng JDoodle API)
        public async Task<RunCodeResponseDto> RunCodeAsync(RunCodeRequestDto request)
        {
            string clientId = _configuration["JDoodleConfig:ClientId"];
            string clientSecret = _configuration["JDoodleConfig:ClientSecret"];
            string apiUrl = "https://api.jdoodle.com/v1/execute";

            // Map ngôn ngữ từ Frontend sang chuẩn của JDoodle
            string jLanguage = request.Language.ToLower() switch
            {
                "csharp" => "csharp",
                "javascript" => "nodejs",
                "python" => "python3",
                "java" => "java",
                _ => "csharp"
            };

            // VersionIndex cho C# (Thường 4 là bản C# hỗ trợ tốt nhất trên JDoodle)
            string jVersion = jLanguage == "csharp" ? "4" : "0";

            // Payload gửi sang JDoodle
            var jdoodleReq = new
            {
                clientId = clientId,
                clientSecret = clientSecret,
                script = request.SourceCode,
                language = jLanguage,
                versionIndex = jVersion,
                stdin = request.Stdin
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(apiUrl, jdoodleReq);
                var resultString = await response.Content.ReadAsStringAsync();

                // Nếu JDoodle sập hoặc từ chối kết nối
                if (!response.IsSuccessStatusCode)
                {
                    return new RunCodeResponseDto { Output = "Lỗi HTTP từ JDoodle: " + resultString, IsError = true };
                }

                // Đọc kết quả JDoodle trả về
                using var jsonDoc = JsonDocument.Parse(resultString);
                var root = jsonDoc.RootElement;

                // Kiểm tra xem JDoodle có báo lỗi API không (ví dụ: Hết lượt chạy miễn phí, Sai API Key)
                if (root.TryGetProperty("error", out var errorEl) && !string.IsNullOrEmpty(errorEl.GetString()))
                {
                    return new RunCodeResponseDto { Output = "JDoodle API Error: " + errorEl.GetString(), IsError = true };
                }

                // Lấy kết quả in ra màn hình (JDoodle gộp chung cả Console.Write và Lỗi Code vào trường output)
                string output = root.TryGetProperty("output", out var outputEl) ? outputEl.GetString() : "Không có output";

                // Trả về cho Frontend
                return new RunCodeResponseDto { Output = output, IsError = false };
            }
            catch (Exception ex)
            {
                return new RunCodeResponseDto
                {
                    Output = $"Không thể kết nối đến JDoodle. Chi tiết lỗi: {ex.Message}",
                    IsError = true
                };
            }
        }

        // Chức năng 37: AI Tutor Chat
        public async Task<string> ChatWithAiTutorAsync(AiTutorRequestDto request)
        {
            // 1. Lấy hoặc tạo Session lưu lịch sử chat cho bài tập này
            var session = await _repository.GetOrCreateAiChatSessionAsync(request.StudentId, "PracticeWorkspace");

            // 2. Lưu câu hỏi của sinh viên vào DB
            await _repository.SaveChatMessageAsync(new ChatMessage
            {
                MessageId = Guid.NewGuid(),
                AiSessionId = session.AiSessionId,
                SenderType = "Student",
                MessageText = request.UserMessage,
                SentAt = DateTime.Now
            });

            // 3. Chuẩn bị Prompt cho Gemini (Ép AI làm Gia Sư)
            string prompt = $@"Bạn là một gia sư lập trình (AI Tutor). 
Nguyên tắc tối thượng: CHỈ GỢI Ý (HINTS), TUYỆT ĐỐI KHÔNG ĐƯỢC VIẾT SẴN CODE GIẢI BÀI CHO HỌC VIÊN.
Hãy dẫn dắt để học viên tự tìm ra tư duy logic.
Đề bài: {request.ProblemDescription}
Code hiện tại của học viên: 
{request.CurrentCode}
Câu hỏi của học viên: {request.UserMessage}
Hãy trả lời ngắn gọn, thân thiện, và đưa ra 1 gợi ý tiếp theo.";

            string apiKey = _configuration["GeminiApiConfig:ApiKey"];
            string baseUrl = _configuration["GeminiApiConfig:BaseUrl"];
            string requestUrl = $"{baseUrl}?key={apiKey}";

            var payload = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };

            string aiResponseText = "Xin lỗi, AI Tutor hiện đang quá tải. Hãy thử lại sau.";

            try
            {
                var response = await _httpClient.PostAsJsonAsync(requestUrl, payload);
                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsStringAsync();
                    var geminiResponse = JsonSerializer.Deserialize<GeminiResponseDto>(responseData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    aiResponseText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? aiResponseText;
                }
            }
            catch { }

            // 4. Lưu câu trả lời của AI vào DB
            await _repository.SaveChatMessageAsync(new ChatMessage
            {
                MessageId = Guid.NewGuid(),
                AiSessionId = session.AiSessionId,
                SenderType = "AI",
                MessageText = aiResponseText,
                SentAt = DateTime.Now
            });

            return aiResponseText;
        }
    }
}