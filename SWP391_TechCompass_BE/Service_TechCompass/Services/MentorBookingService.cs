using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs;
using Service_TechCompass.Hubs;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class MentorBookingService : IMentorBookingService
    {
        private readonly IMentorBookingRepository _repo;
        private readonly IHubContext<MentorChatHub> _hubContext; // SignalR Context

        public MentorBookingService(IMentorBookingRepository repo, IHubContext<MentorChatHub> hubContext)
        {
            _repo = repo;
            _hubContext = hubContext;
        }

        // Task 59 & 60: Lấy danh sách Mentor
        public async Task<List<MentorDto>> GetMentorsAsync(MentorFilterDto filter)
        {
            var mentors = await _repo.GetMentorsAsync(filter.Keyword, filter.ExpertiseTags);
            return mentors.Select(m => new MentorDto
            {
                MentorId = m.MentorId,
                Email = m.User?.Email,
                CurrentCompany = m.CurrentCompany,
                ExpertiseTags = m.ExpertiseTags,
                LinkedinUrl = m.LinkedinUrl
            }).ToList();
        }

        // Task 61 & 66: Đặt lịch và bắn Notification
        public async Task<Guid> BookMentorAsync(Guid studentId, BookMentorRequestDto request)
        {
            var session = new MentorSession
            {
                SessionId = Guid.NewGuid(),
                StudentId = studentId,
                MentorId = request.MentorId,
                ScheduledAt = request.ScheduledAt,
                DurationMinutes = request.DurationMinutes,
                Status = "PENDING",
                PaymentStatus = "UNPAID"
            };

            await _repo.CreateMentorSessionAsync(session);

            // Task 66: Bắn thông báo Realtime cho Mentor biết có người đặt lịch
            await _hubContext.Clients.Group(request.MentorId.ToString())
                .SendAsync("ReceiveNotification", "Bạn có một yêu cầu đặt lịch mới từ sinh viên!");

            return session.SessionId;
        }

        // Task 62: Sinh viên thanh toán thành công
        public async Task UpdatePaymentStatusAsync(Guid sessionId, string paymentStatus)
        {
            var session = await _repo.GetSessionByIdAsync(sessionId);
            if (session != null)
            {
                session.PaymentStatus = paymentStatus;
                await _repo.UpdateSessionAsync(session);
            }
        }

        // Task 63 & 66: Mentor duyệt lịch và bắn Notification cho SV
        public async Task ApproveBookingAsync(Guid sessionId, string meetingLink)
        {
            var session = await _repo.GetSessionByIdAsync(sessionId);
            if (session == null) throw new Exception("Không tìm thấy phiên Mentor.");

            session.Status = "APPROVED";
            session.MeetingLink = meetingLink;
            await _repo.UpdateSessionAsync(session);

            // Task 66: Bắn thông báo Realtime cho Sinh viên
            await _hubContext.Clients.Group(session.StudentId.ToString())
                .SendAsync("ReceiveNotification", $"Lịch hẹn của bạn đã được Mentor duyệt! Link Meet: {meetingLink}");
        }

        // Task 64: Mentor note lại đánh giá sau buổi học
        public async Task ReviewNoteAsync(Guid sessionId, string reviewNote)
        {
            var session = await _repo.GetSessionByIdAsync(sessionId);
            if (session != null)
            {
                session.ReviewNotes = reviewNote;
                session.Status = "COMPLETED"; // Đánh dấu hoàn thành
                await _repo.UpdateSessionAsync(session);
            }
        }

        // Task 65: Sinh viên / Mentor Chat Realtime trong phiên
        public async Task SendMessageAsync(Guid sessionId, ChatMessageRequestDto request)
        {
            var message = new ChatMessage
            {
                MessageId = Guid.NewGuid(),
                MentorSessionId = sessionId,
                SenderType = request.SenderType,
                MessageText = request.MessageText,
                SentAt = DateTime.Now
            };

            // 1. Lưu vào DB để xem lại lịch sử
            await _repo.SaveChatMessageAsync(message);

            // 2. Bắn SignalR Realtime tới Group của Session này để cả 2 bên cùng thấy tin nhắn nảy lên
            await _hubContext.Clients.Group(sessionId.ToString())
                .SendAsync("ReceiveMessage", request.SenderType, request.MessageText, message.SentAt);
        }
    }
}
