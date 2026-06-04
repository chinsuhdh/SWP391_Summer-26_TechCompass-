using Repository_TechCompass.Models;

namespace Repository_TechCompass.Interfaces
{
    public interface IPracticeWorkspaceRepository
    {
        Task<AiChatSession> GetOrCreateAiChatSessionAsync(Guid studentId, string contextType);
        Task SaveChatMessageAsync(ChatMessage message);
    }
}