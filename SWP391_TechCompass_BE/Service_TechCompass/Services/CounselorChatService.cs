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
            // BƯỚC 1: Tìm StudentId chuẩn
            var actualStudent = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == studentId || s.StudentId == studentId);
            if (actualStudent == null) throw new Exception("Không tìm thấy hồ sơ sinh viên.");

            // BƯỚC 2 (ĐÃ SỬA): Tìm CounselorId chuẩn từ UserId gửi lên
            var actualCounselor = await _context.Counselors
                .FirstOrDefaultAsync(c => c.UserId == counselorId || c.CounselorId == counselorId);
            if (actualCounselor == null) throw new Exception("Không tìm thấy thông tin Cố vấn.");

            var finalStudentId = actualStudent.StudentId;
            var finalCounselorId = actualCounselor.CounselorId;

            // BƯỚC 3: Kiểm tra Session hiện tại
            var session = await _context.CounselorSessions
                .FirstOrDefaultAsync(s => s.StudentId == finalStudentId && s.CounselorId == finalCounselorId && s.Status == "Open");

            if (session == null)
            {
                session = new CounselorSession
                {
                    SessionId = Guid.NewGuid(),
                    StudentId = finalStudentId,
                    CounselorId = finalCounselorId,
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
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }
    }
}