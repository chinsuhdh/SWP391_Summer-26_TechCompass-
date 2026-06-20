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

        public async Task<List<ChatMessage>> GetRecentMessagesAsync(Guid sessionId, int takeCount)
        {
            return await _context.ChatMessages
                .Where(m => m.AiSessionId == sessionId)
                .OrderByDescending(m => m.SentAt) 
                .Take(takeCount)
                .ToListAsync();
        }

        public async Task<List<AiChatSession>> GetStudentSessionsAsync(Guid studentId, string contextType)
        {
            return await _context.AiChatSessions
                .Where(s => s.StudentId == studentId && s.ContextType == contextType)
                .OrderByDescending(s => s.StartedAt) // Xếp chat mới nhất lên đầu
                .ToListAsync();
        }

        public async Task<AiChatSession> CreateNewAiChatSessionAsync(Guid studentId, string contextType)
        {
            var newSession = new AiChatSession
            {
                AiSessionId = Guid.NewGuid(),
                StudentId = studentId,
                ContextType = contextType,
                StartedAt = DateTime.Now
            };
            _context.AiChatSessions.Add(newSession);
            await _context.SaveChangesAsync();
            return newSession;
        }
    }
}