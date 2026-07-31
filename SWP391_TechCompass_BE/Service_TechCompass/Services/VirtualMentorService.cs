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

        // 1. ĐỔI SANG DÙNG IStudentRepository
        private readonly IStudentRepository _studentRepo;

        // 2. Tiêm IStudentRepository qua Constructor
        public VirtualMentorService(
            IPracticeWorkspaceRepository repository,
            Kernel kernel,
            IStudentRepository studentRepo)
        {
            _repository = repository;
            _chatCompletionService = kernel.GetRequiredService<IChatCompletionService>("GeminiChat");
            _studentRepo = studentRepo;
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

            // ---------------------------------------------------------
            // 🔴 BƯỚC QUAN TRỌNG: Lấy dữ liệu ngữ cảnh bằng IStudentRepository
            // ---------------------------------------------------------
            // Dùng hàm bất đồng bộ có sẵn trong IStudentRepository
            var student = await _studentRepo.GetStudentByIdAsync(studentId);

            // Lấy thông tin cá nhân (Bắt lỗi null nếu sinh viên chưa có dữ liệu)
            string studentName = student?.FullName ?? "Sinh viên ẩn danh";

            // Lấy tên vai trò nghề nghiệp (Ví dụ: Backend Developer) thay vì lấy object
            string targetRole = student?.TargetRole?.RoleName ?? "Chưa xác định";
            string currentSkills = "Chưa có dữ liệu"; // Bạn có thể bổ sung truy vấn kỹ năng sau

            // Xây dựng System Prompt Động
            string systemPrompt = $@"Bạn là một Cố vấn Hướng nghiệp IT cấp cao (Senior Career Mentor) thuộc nền tảng TechCompass.
Nhiệm vụ của bạn là tư vấn cá nhân hóa cho sinh viên dựa trên hồ sơ thực tế của họ.

THÔNG TIN HỒ SƠ CỦA SINH VIÊN HIỆN TẠI:
- Tên sinh viên: {studentName}
- Mục tiêu nghề nghiệp hướng tới: {targetRole}
- Các kỹ năng đang có: {currentSkills}

NGUYÊN TẮC TƯ VẤN:
1. Luôn xưng hô thân thiện và gọi tên sinh viên ({studentName}) trong câu trả lời nếu phù hợp.
2. Dựa trên 'Mục tiêu nghề nghiệp' và 'Kỹ năng đang có' ở trên, hãy phân tích những kỹ năng còn thiếu (Skill Gap) so với yêu cầu thị trường hiện nay.
3. Chỉ tư vấn định hướng, tuyệt đối KHÔNG viết code hay giải bài tập giúp sinh viên. Nếu sinh viên hỏi sai chủ đề, hãy từ chối khéo léo.";

            chatHistory.AddSystemMessage(systemPrompt);

            // 4. Nạp lịch sử chat cũ vào Prompt (10 tin nhắn gần nhất để AI nhớ luồng trò chuyện)
            var previousMessages = await _repository.GetRecentMessagesAsync(currentSessionId, 10);

            foreach (var msg in previousMessages.OrderBy(m => m.SentAt))
            {
                if (msg.SenderType == "Student")
                    chatHistory.AddUserMessage(msg.MessageText);
                else if (msg.SenderType == "AI")
                    chatHistory.AddAssistantMessage(msg.MessageText);
            }

            // Đưa tin nhắn mới nhất của sinh viên vào
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
                // Catch để tránh crash server nếu timeout từ API Google
            }

            // 6. Lưu câu trả lời của AI vào Database
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