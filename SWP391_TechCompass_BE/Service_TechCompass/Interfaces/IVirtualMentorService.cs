using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Service_TechCompass.DTOs.VirtualMentor;

namespace Service_TechCompass.Interfaces
{
    public interface IVirtualMentorService
    {
        Task<List<ChatSessionListDto>> GetUserSessionsAsync(Guid studentId);
        Task<(Guid SessionId, string AiResponse)> ChatAsync(Guid studentId, string userMessage, Guid? sessionId);
        Task<List<ChatHistoryResponseDto>> GetChatHistoryAsync(Guid sessionId);

        // THÊM DÒNG NÀY: Hàm xóa đoạn chat
        Task<bool> DeleteSessionAsync(Guid sessionId, Guid studentId);
    }
}