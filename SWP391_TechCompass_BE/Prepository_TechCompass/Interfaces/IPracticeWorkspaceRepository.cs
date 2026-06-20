using Repository_TechCompass.Models;

namespace Repository_TechCompass.Interfaces
{
    public interface IPracticeWorkspaceRepository
    {
        Task<AiChatSession> GetOrCreateAiChatSessionAsync(Guid studentId, string contextType);
        Task SaveChatMessageAsync(ChatMessage message);
        Task<List<ChatMessage>> GetRecentMessagesAsync(Guid sessionId, int takeCount);

        Task<List<AiChatSession>> GetStudentSessionsAsync(Guid studentId, string contextType);
        Task<AiChatSession> CreateNewAiChatSessionAsync(Guid studentId, string contextType);
    }
}