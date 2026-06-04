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

        // Chức năng 35: Run Code
        public async Task<RunCodeResponseDto> RunCodeAsync(RunCodeRequestDto request)
        {
            string jBaseUrl = _configuration["Judge0Config:BaseUrl"];
            string jApiKey = _configuration["Judge0Config:ApiKey"];
            string jApiHost = _configuration["Judge0Config:ApiHost"];

            int langId = request.Language.ToLower() switch
            {
                "csharp" => 51,
                "javascript" => 63,
                "python" => 71,
                "java" => 62,
                _ => 51
            };

            var judge0Req = new
            {
                source_code = request.SourceCode,
                language_id = langId,
                stdin = request.Stdin
            };

            var jRequestMessage = new HttpRequestMessage(HttpMethod.Post, jBaseUrl);
            jRequestMessage.Headers.Add("X-RapidAPI-Key", jApiKey);
            jRequestMessage.Headers.Add("X-RapidAPI-Host", jApiHost);
            jRequestMessage.Content = new StringContent(JsonSerializer.Serialize(judge0Req), Encoding.UTF8, "application/json");

            var jResponse = await _httpClient.SendAsync(jRequestMessage);

            if (!jResponse.IsSuccessStatusCode)
            {
                return new RunCodeResponseDto { Output = "Lỗi kết nối tới server biên dịch Judge0.", IsError = true };
            }

            var jResultString = await jResponse.Content.ReadAsStringAsync();
            var jResult = JsonSerializer.Deserialize<Judge0ResponseDto>(jResultString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            bool isError = jResult?.Status?.Id != 3; // ID 3 = Accepted
            string output = isError
                ? (jResult?.CompileOutput ?? jResult?.StdErr ?? "Lỗi không xác định khi chạy code.")
                : (jResult?.StdOut ?? "Chương trình chạy thành công, không có output.");

            return new RunCodeResponseDto { Output = output, IsError = isError };
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