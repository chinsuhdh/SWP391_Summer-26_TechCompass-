using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Service_TechCompass.DTOs.VirtualMentor;

namespace Service_TechCompass.Interfaces
{
    public interface IVirtualMentorService
    {
        // Lấy danh sách các cuộc trò chuyện để hiển thị ở Sidebar
        Task<List<ChatSessionListDto>> GetUserSessionsAsync(Guid studentId);

        // Chat với AI, trả về SessionId (hữu ích khi vừa tạo session mới) và câu trả lời
        Task<(Guid SessionId, string AiResponse)> ChatAsync(Guid studentId, string userMessage, Guid? sessionId);

        // Lấy lịch sử chat theo từng Session cụ thể
        Task<List<ChatHistoryResponseDto>> GetChatHistoryAsync(Guid sessionId);
    }
}