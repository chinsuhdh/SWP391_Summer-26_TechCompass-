using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
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
        private readonly IChatCompletionService _chatCompletionService;
        private readonly IMemoryCache _cache; // Inject Cache để chặn gọi API lặp lại

        public PracticeWorkspaceService(
            IPracticeWorkspaceRepository repository,
            HttpClient httpClient,
            IConfiguration configuration,
            Kernel kernel,
            IMemoryCache cache)
        {
            _repository = repository;
            _httpClient = httpClient;
            _configuration = configuration;
            _cache = cache;
            _chatCompletionService = kernel.GetRequiredService<IChatCompletionService>("GeminiChat");
        }

        public async Task<RunCodeResponseDto> RunCodeAsync(RunCodeRequestDto request)
        {
            // 1. TẠO CACHE KEY DỰA TRÊN CODE + NGÔN NGỮ + INPUT
            string sourceHash = request.SourceCode != null ? request.SourceCode.GetHashCode().ToString() : "EMPTY";
            string stdinHash = request.Stdin != null ? request.Stdin.GetHashCode().ToString() : "EMPTY";
            string cacheKey = $"RUN_CODE_{request.Language?.ToLower()}_{sourceHash}_{stdinHash}";

            // 2. NẾU USER BẤM LIÊN TỤC MÀ CHƯA SỬA CODE -> TRẢ VỀ KẾT QUẢ CACHE NGAY (TRÁNH GỌI JDOODLE)
            if (_cache.TryGetValue(cacheKey, out RunCodeResponseDto? cachedResponse) && cachedResponse != null)
            {
                return cachedResponse;
            }

            string clientId = _configuration["JDoodleConfig:ClientId"];
            string clientSecret = _configuration["JDoodleConfig:ClientSecret"];
            string apiUrl = "https://api.jdoodle.com/v1/execute";

            string jLanguage = request.Language?.ToLower() switch
            {
                "csharp" => "csharp",
                "javascript" => "nodejs",
                "python" => "python3",
                "java" => "java",
                "sql" => "sql",
                "bash" => "bash",
                "cpp" => "cpp14",
                _ => "csharp"
            };

            string jVersion = jLanguage switch
            {
                "csharp" => "4",
                "sql" => "0",
                "bash" => "0",
                "cpp14" => "4",
                _ => "0"
            };

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

                string output = string.Empty;
                if (root.TryGetProperty("output", out var outputEl))
                {
                    output = outputEl.GetString() ?? "";
                }
                else if (root.TryGetProperty("stdout", out var stdoutEl))
                {
                    output = stdoutEl.GetString() ?? "";
                }

                var finalResult = new RunCodeResponseDto
                {
                    Output = string.IsNullOrEmpty(output) ? "Không có output (Chạy thành công)." : output,
                    IsError = false
                };

                // 3. LƯU CACHE 2 PHÚT NẾU CHẠY THÀNH CÔNG
                _cache.Set(cacheKey, finalResult, TimeSpan.FromMinutes(2));

                return finalResult;
            }
            catch (Exception ex)
            {
                return new RunCodeResponseDto
                {
                    Output = $"Không thể kết nối đến JDoodle hoặc hệ thống biên dịch đang bận. Chi tiết: {ex.Message}",
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

            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage("Bạn là một gia sư lập trình (AI Tutor). Nguyên tắc tối thượng: CHỈ GỢI Ý (HINTS), TUYỆT ĐỐI KHÔNG ĐƯỢC VIẾT SẴN CODE GIẢI BÀI CHO HỌC VIÊN. Hãy dẫn dắt để học viên tự tìm ra tư duy logic.");

            string prompt = $@"Đề bài: {request.ProblemDescription}
Code hiện tại của học viên: 
{request.CurrentCode}
Câu hỏi của học viên: {request.UserMessage}
Hãy trả lời ngắn gọn, thân thiện bằng tiếng Việt, và đưa ra 1 gợi ý tiếp theo.";
            chatHistory.AddUserMessage(prompt);

            string aiResponseText = "Xin lỗi, AI Tutor hiện đang quá tải hoặc tạm thời không thể phản hồi. Bạn hãy thử lại sau ít phút nhé.";

            try
            {
                var response = await _chatCompletionService.GetChatMessageContentAsync(chatHistory);
                aiResponseText = response.ToString() ?? aiResponseText;
            }
            catch
            {
                // Bỏ qua lỗi kết nối hệ thống AI để bảo vệ ứng dụng không bị crash
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