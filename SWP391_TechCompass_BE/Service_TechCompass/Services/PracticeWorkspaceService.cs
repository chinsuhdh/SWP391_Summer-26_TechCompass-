using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs;
using Service_TechCompass.DTOs.Assessment;
using Service_TechCompass.DTOs.Practice;
using Service_TechCompass.Interfaces;
// THÊM NAMESPACE SEMANTIC KERNEL
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Service_TechCompass.Services
{
    public class PracticeWorkspaceService : IPracticeWorkspaceService
    {
        private readonly IPracticeWorkspaceRepository _repository;
        private readonly HttpClient _httpClient; // Giữ lại cho JDoodle
        private readonly IConfiguration _configuration;
        private readonly IChatCompletionService _chatCompletionService; // Khai báo bộ dịch vụ của SK

        public PracticeWorkspaceService(IPracticeWorkspaceRepository repository, HttpClient httpClient, IConfiguration configuration, Kernel kernel)
        {
            _repository = repository;
            _httpClient = httpClient;
            _configuration = configuration;
            // Trích xuất service chat từ DI Container thông qua ID định danh
            _chatCompletionService = kernel.GetRequiredService<IChatCompletionService>("GeminiChat");
        }

        public async Task<RunCodeResponseDto> RunCodeAsync(RunCodeRequestDto request)
        {
            string clientId = _configuration["JDoodleConfig:ClientId"];
            string clientSecret = _configuration["JDoodleConfig:ClientSecret"];
            string apiUrl = "https://api.jdoodle.com/v1/execute";

            string jLanguage = request.Language.ToLower() switch
            {
                "csharp" => "csharp",
                "javascript" => "nodejs",
                "python" => "python3",
                "java" => "java",
                _ => "csharp"
            };

            string jVersion = jLanguage == "csharp" ? "4" : "0";

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

                if (!response.IsSuccessStatusCode)
                {
                    return new RunCodeResponseDto { Output = "Lỗi HTTP từ JDoodle: " + resultString, IsError = true };
                }

                using var jsonDoc = JsonDocument.Parse(resultString);
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("error", out var errorEl) && !string.IsNullOrEmpty(errorEl.GetString()))
                {
                    return new RunCodeResponseDto { Output = "JDoodle API Error: " + errorEl.GetString(), IsError = true };
                }

                string output = root.TryGetProperty("output", out var outputEl) ? outputEl.GetString() : "Không có output";
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

        public async Task<string> ChatWithAiTutorAsync(AiTutorRequestDto request)
        {
            var session = await _repository.GetOrCreateAiChatSessionAsync(request.StudentId, "PracticeWorkspace");

            await _repository.SaveChatMessageAsync(new ChatMessage
            {
                MessageId = Guid.NewGuid(),
                AiSessionId = session.AiSessionId,
                SenderType = "Student",
                MessageText = request.UserMessage,
                SentAt = DateTime.Now
            });

            // Sử dụng ChatHistory để kiểm soát nghiêm ngặt AI Tutor đóng đúng vai trò sư phạm
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage("Bạn là một gia sư lập trình (AI Tutor). Nguyên tắc tối thượng: CHỈ GỢI Ý (HINTS), TUYỆT ĐỐI KHÔNG ĐƯỢC VIẾT SẴN CODE GIẢI BÀI CHO HỌC VIÊN. Hãy dẫn dắt để học viên tự tìm ra tư duy logic.");

            string prompt = $@"Đề bài: {request.ProblemDescription}
Code hiện tại của học viên: 
{request.CurrentCode}
Câu hỏi của học viên: {request.UserMessage}
Hãy trả lời ngắn gọn, thân thiện bằng tiếng Việt, và đưa ra 1 gợi ý tiếp theo.";
            chatHistory.AddUserMessage(prompt);

            string aiResponseText = "Xin lỗi, AI Tutor hiện đang quá tải. Hãy thử lại sau.";

            try
            {
                // Thực thi gọi API thông qua Semantic Kernel
                var response = await _chatCompletionService.GetChatMessageContentAsync(chatHistory);
                aiResponseText = response.ToString() ?? aiResponseText;
            }
            catch
            {
                // Bỏ qua lỗi kết nối hệ thống AI để bảo vệ app không crash
            }

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