using Microsoft.EntityFrameworkCore;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;

namespace Repository_TechCompass.Repositories
{
    public class PracticeWorkspaceRepository : IPracticeWorkspaceRepository
    {
        private readonly Swp391CareerRoadmapContext _context;

        public PracticeWorkspaceRepository(Swp391CareerRoadmapContext context)
        {
            _context = context;
        }

        public async Task<AiChatSession> GetOrCreateAiChatSessionAsync(Guid studentId, string contextType)
        {
            var session = await _context.AiChatSessions
                .FirstOrDefaultAsync(s => s.StudentId == studentId && s.ContextType == contextType);

            if (session == null)
            {
                session = new AiChatSession
                {
                    AiSessionId = Guid.NewGuid(),
                    StudentId = studentId,
                    ContextType = contextType,
                    StartedAt = DateTime.Now
                };
                _context.AiChatSessions.Add(session);
                await _context.SaveChangesAsync();
            }
            return session;
        }

        public async Task SaveChatMessageAsync(ChatMessage message)
        {
            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();
        }
    }
}