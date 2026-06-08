using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;

namespace Repository_TechCompass.Repositories
{
    public class MentorBookingRepository : IMentorBookingRepository
    {
        private readonly Swp391CareerRoadmapContext _context;

        public MentorBookingRepository(Swp391CareerRoadmapContext context)
        {
            _context = context;
        }

        public async Task<List<Mentor>> GetMentorsAsync(string? keyword, string? tags)
        {
            var query = _context.Mentors.Include(m => m.User).AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(m => m.CurrentCompany!.Contains(keyword) || m.User.Email.Contains(keyword));

            if (!string.IsNullOrWhiteSpace(tags))
                query = query.Where(m => m.ExpertiseTags!.Contains(tags));

            return await query.ToListAsync();
        }

        public async Task<MentorSession> CreateMentorSessionAsync(MentorSession session)
        {
            _context.MentorSessions.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }

        public async Task<MentorSession?> GetSessionByIdAsync(Guid sessionId)
        {
            return await _context.MentorSessions
                .Include(s => s.Mentor)
                .Include(s => s.Student)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);
        }

        public async Task UpdateSessionAsync(MentorSession session)
        {
            _context.MentorSessions.Update(session);
            await _context.SaveChangesAsync();
        }

        public async Task SaveChatMessageAsync(ChatMessage message)
        {
            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ChatMessage>> GetChatHistoryAsync(Guid sessionId)
        {
            return await _context.ChatMessages
                .Where(c => c.MentorSessionId == sessionId)
                .OrderBy(c => c.SentAt)
                .ToListAsync();
        }
    }
}