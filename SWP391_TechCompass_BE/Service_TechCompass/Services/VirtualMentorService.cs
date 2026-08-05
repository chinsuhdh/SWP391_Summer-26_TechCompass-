using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Repository_TechCompass;
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
        private readonly IStudentRepository _studentRepo;
        private readonly IAssessmentService _assessmentService;
        private readonly Swp391CareerRoadmapContext _context; // ĐÃ THÊM: DbContext để xóa trực tiếp

        public VirtualMentorService(
            IPracticeWorkspaceRepository repository,
            Kernel kernel,
            IStudentRepository studentRepo,
            IAssessmentService assessmentService,
            Swp391CareerRoadmapContext context) // ĐÃ THÊM: Tiêm DbContext vào constructor
        {
            _repository = repository;
            _chatCompletionService = kernel.GetRequiredService<IChatCompletionService>("GeminiChat");
            _studentRepo = studentRepo;
            _assessmentService = assessmentService;
            _context = context;
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

            if (sessionId.HasValue && sessionId.Value != Guid.Empty)
            {
                currentSessionId = sessionId.Value;
            }
            else
            {
                var newSession = await _repository.CreateNewAiChatSessionAsync(studentId, "CareerMentor");
                currentSessionId = newSession.AiSessionId;
            }

            await _repository.SaveChatMessageAsync(new ChatMessage
            {
                MessageId = Guid.NewGuid(),
                AiSessionId = currentSessionId,
                SenderType = "Student",
                MessageText = userMessage,
                SentAt = DateTime.Now
            });

            var chatHistory = new ChatHistory();

            var student = await _studentRepo.GetStudentByIdAsync(studentId);
            string studentName = student?.FullName ?? "Sinh viên ẩn danh";
            string targetRole = student?.TargetRole?.RoleName ?? "Chưa xác định";

            string currentSkills = "";
            try
            {
                var history = await _assessmentService.GetMyAssessmentHistoryListAsync(studentId);
                if (history != null && history.Any())
                {
                    currentSkills = JsonSerializer.Serialize(history);
                }
                else
                {
                    currentSkills = "Sinh viên chưa làm bài kiểm tra năng lực nào.";
                }
            }
            catch
            {
                currentSkills = "Không thể tải dữ liệu kỹ năng lúc này.";
            }

            string systemPrompt = $@"Bạn là một Cố vấn Hướng nghiệp IT cấp cao (Senior Career Mentor) thuộc nền tảng TechCompass.
Nhiệm vụ của bạn là tư vấn cá nhân hóa cho sinh viên dựa trên hồ sơ thực tế của họ.

THÔNG TIN HỒ SƠ TỪ CƠ SỞ DỮ LIỆU CỦA SINH VIÊN HIỆN TẠI:
- Tên sinh viên: {studentName}
- Mục tiêu nghề nghiệp hướng tới: {targetRole}
- Trạng thái kỹ năng / Lịch sử đánh giá: {currentSkills}

NGUYÊN TẮC TƯ VẤN:
1. Luôn xưng hô thân thiện và gọi tên sinh viên ({studentName}) trong câu trả lời nếu phù hợp.
2. Dựa trên 'Mục tiêu nghề nghiệp' và 'Trạng thái kỹ năng' ở trên, hãy phân tích trực tiếp. Khen ngợi nếu điểm kỹ năng cao, nhắc nhở học thêm và vạch ra định hướng (Skill Gap) nếu điểm thấp hoặc còn thiếu.
3. Chỉ tư vấn định hướng, tuyệt đối KHÔNG viết code hay giải bài tập giúp sinh viên. Nếu sinh viên hỏi sai chủ đề, hãy từ chối khéo léo.";

            chatHistory.AddSystemMessage(systemPrompt);

            var previousMessages = await _repository.GetRecentMessagesAsync(currentSessionId, 10);

            foreach (var msg in previousMessages.OrderBy(m => m.SentAt))
            {
                if (msg.SenderType == "Student")
                    chatHistory.AddUserMessage(msg.MessageText);
                else if (msg.SenderType == "AI")
                    chatHistory.AddAssistantMessage(msg.MessageText);
            }

            chatHistory.AddUserMessage(userMessage);

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

        // HÀM XÓA ĐOẠN CHAT ĐÃ FIX DÙNG DB CONTEXT TRỰC TIẾP
        public async Task<bool> DeleteSessionAsync(Guid sessionId, Guid studentId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Tìm phiên chat dựa vào ID và đảm bảo đúng thuộc về sinh viên này (bảo mật)
                var session = await _context.AiChatSessions
                    .FirstOrDefaultAsync(s => s.AiSessionId == sessionId && s.StudentId == studentId);

                if (session == null)
                {
                    return false; // Không tìm thấy hoặc không có quyền xóa
                }

                // 2. Xóa tất cả tin nhắn liên quan đến phiên chat này trước (tránh lỗi khóa ngoại)
                var messages = await _context.ChatMessages
                    .Where(m => m.AiSessionId == sessionId)
                    .ToListAsync();

                if (messages.Any())
                {
                    _context.ChatMessages.RemoveRange(messages);
                }

                // 3. Xóa chính phiên chat đó
                _context.AiChatSessions.Remove(session);

                // 4. Lưu thay đổi vào Database
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Lỗi xóa session: {ex.Message}");
                return false;
            }
        }
    }
}