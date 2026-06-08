using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Repository_TechCompass.Models;

namespace Repository_TechCompass.Interfaces
{
    public interface IMentorBookingRepository
    {
        Task<List<Mentor>> GetMentorsAsync(string? keyword, string? tags);
        Task<MentorSession> CreateMentorSessionAsync(MentorSession session);
        Task<MentorSession?> GetSessionByIdAsync(Guid sessionId);
        Task UpdateSessionAsync(MentorSession session);
        Task SaveChatMessageAsync(ChatMessage message);
        Task<List<ChatMessage>> GetChatHistoryAsync(Guid sessionId);
    }
}