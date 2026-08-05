using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Repository_TechCompass.Models;

namespace Service_TechCompass.Interfaces
{
    public interface ICounselorChatService
    {
        Task<CounselorSession> GetOrCreateSessionAsync(Guid studentId, Guid counselorId);
        Task<ChatMessage> SaveMessageAsync(Guid sessionId, Guid senderId, string content, bool isFromStudent);
        Task<List<ChatMessage>> GetChatHistoryAsync(Guid sessionId);
    }
}