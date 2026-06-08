using System;
using System.Collections.Generic;

namespace Service_TechCompass.DTOs
{
    // Task 59, 60: Xem danh sách và Filter Mentor
    public class MentorFilterDto
    {
        public string? Keyword { get; set; }
        public string? ExpertiseTags { get; set; }
    }


    // Task 61: Booking Mentor
    public class BookMentorRequestDto
    {
        public Guid MentorId { get; set; }
        public DateTime ScheduledAt { get; set; }
        public int DurationMinutes { get; set; } = 60;
    }

    // Task 62, 63, 64: Cập nhật Session
    public class UpdatePaymentStatusDto { public string PaymentStatus { get; set; } = "PAID"; }
    public class ApproveBookingDto { public string MeetingLink { get; set; } = null!; }
    public class ReviewNoteDto { public string ReviewNotes { get; set; } = null!; }

    // Task 65: Chat Realtime
    public class ChatMessageRequestDto
    {
        public string SenderType { get; set; } = null!; // "Student" hoặc "Mentor"
        public string MessageText { get; set; } = null!;
    }
}