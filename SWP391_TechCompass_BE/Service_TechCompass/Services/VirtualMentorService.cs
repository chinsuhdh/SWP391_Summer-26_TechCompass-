using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs.VirtualMentor;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class VirtualMentorService : IVirtualMentorService
    {
        private readonly IPracticeWorkspaceRepository _repository;
        private readonly IChatCompletionService _chatCompletionService;

        public VirtualMentorService(IPracticeWorkspaceRepository repository, Kernel kernel)
        {
            _repository = repository;
            _chatCompletionService = kernel.GetRequiredService<IChatCompletionService>("GeminiChat");
        }

        public async Task<List<ChatSessionListDto>> GetUserSessionsAsync(Guid studentId)
        {
            var sessions = await _repository.GetStudentSessionsAsync(studentId, "CareerMentor");
            return sessions.Select(s => new ChatSessionListDto
            {
                SessionId = s.AiSessionId,
                StartedAt = s.StartedAt ?? DateTime.Now,
                Title = $"Tư vấn ngày {s.StartedAt ?? DateTime.Now:dd/MM HH:mm}"
            }).ToList();
        }

        public async Task<(Guid SessionId, string AiResponse)> ChatAsync(Guid studentId, string userMessage, Guid? sessionId)
        {
            Guid currentSessionId;

            // 1. Xác định Session
            if (sessionId.HasValue && sessionId.Value != Guid.Empty)
            {
                currentSessionId = sessionId.Value;
            }
            else
            {
                // Nếu Frontend không truyền ID lên -> Tạo phiên chat mới cứng
                var newSession = await _repository.CreateNewAiChatSessionAsync(studentId, "CareerMentor");
                currentSessionId = newSession.AiSessionId;
            }

            // 2. Lưu tin nhắn của sinh viên vào Database
            await _repository.SaveChatMessageAsync(new ChatMessage
            {
                MessageId = Guid.NewGuid(),
                AiSessionId = currentSessionId,
                SenderType = "Student",
                MessageText = userMessage,
                SentAt = DateTime.Now
            });

            // 3. Khởi tạo Semantic Kernel Chat History
            var chatHistory = new ChatHistory();

            string systemPrompt = @"Bạn là một Cố vấn Hướng nghiệp IT cấp cao (Senior Career Mentor). 
Nhiệm vụ của bạn là tư vấn cho sinh viên ngành Software Engineering. 
Dựa trên thông tin họ cung cấp, hãy chỉ ra các kỹ năng còn thiếu (Skill Gap) so với yêu cầu thị trường 
và gợi ý lộ trình học tập (Roadmap) thực tế. 
Nguyên tắc: Chỉ tư vấn định hướng, tuyệt đối KHÔNG viết code hay giải bài tập giúp sinh viên.";

            chatHistory.AddSystemMessage(systemPrompt);

            // 4. Nạp lịch sử chat cũ vào Prompt
            var previousMessages = await _repository.GetRecentMessagesAsync(currentSessionId, 10);

            foreach (var msg in previousMessages.OrderBy(m => m.SentAt))
            {
                if (msg.SenderType == "Student")
                    chatHistory.AddUserMessage(msg.MessageText);
                else if (msg.SenderType == "AI")
                    chatHistory.AddAssistantMessage(msg.MessageText);
            }

            chatHistory.AddUserMessage(userMessage);

            // 5. Gọi Gemini API
            string aiResponseText = "Xin lỗi, Cố vấn AI hiện đang quá tải. Vui lòng thử lại sau.";
            try
            {
                var response = await _chatCompletionService.GetChatMessageContentAsync(chatHistory);
                aiResponseText = response.ToString() ?? aiResponseText;
            }
            catch
            {
                // Catch để tránh crash server nếu timeout
            }

            // 6. Lưu câu trả lời của AI
            await _repository.SaveChatMessageAsync(new ChatMessage
            {
                MessageId = Guid.NewGuid(),
                AiSessionId = currentSessionId,
                SenderType = "AI",
                MessageText = aiResponseText,
                SentAt = DateTime.Now
            });

            return (currentSessionId, aiResponseText);
        }

        public async Task<List<ChatHistoryResponseDto>> GetChatHistoryAsync(Guid sessionId)
        {
            var messages = await _repository.GetRecentMessagesAsync(sessionId, 50);

            return messages.OrderBy(m => m.SentAt).Select(m => new ChatHistoryResponseDto
            {
                MessageId = m.MessageId,
                SenderType = m.SenderType,
                MessageText = m.MessageText,
                SentAt = m.SentAt ?? DateTime.Now
            }).ToList();
        }
    }
}