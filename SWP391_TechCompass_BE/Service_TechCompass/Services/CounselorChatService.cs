using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Repository_TechCompass;
using Repository_TechCompass.Models;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class CounselorChatService : ICounselorChatService
    {
        private readonly Swp391CareerRoadmapContext _context;

        public CounselorChatService(Swp391CareerRoadmapContext context)
        {
            _context = context;
        }

        public async Task<CounselorSession> GetOrCreateSessionAsync(Guid studentId, Guid counselorId)
        {
            var session = await _context.CounselorSessions
                .FirstOrDefaultAsync(s => s.StudentId == studentId && s.CounselorId == counselorId && s.Status == "Open");

            if (session == null)
            {
                session = new CounselorSession
                {
                    SessionId = Guid.NewGuid(),
                    StudentId = studentId,
                    CounselorId = counselorId,
                    StartedAt = DateTime.Now,
                    Status = "Open"
                };
                _context.CounselorSessions.Add(session);
                await _context.SaveChangesAsync();
            }

            return session;
        }

        public async Task<ChatMessage> SaveMessageAsync(Guid sessionId, Guid senderId, string content, bool isFromStudent)
        {
            var message = new ChatMessage
            {
                MessageId = Guid.NewGuid(),
                CounselorSessionId = sessionId,
                // Map đúng tên thuộc tính trong Model ChatMessage của bạn
                MessageText = content,
                SentAt = DateTime.Now,
                SenderType = isFromStudent ? "Student" : "Counselor"
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            return message;
        }

        public async Task<List<ChatMessage>> GetChatHistoryAsync(Guid sessionId)
        {
            return await _context.ChatMessages
                .Where(m => m.CounselorSessionId == sessionId)
                // Map đúng thuộc tính SentAt để sắp xếp
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }
    }
}