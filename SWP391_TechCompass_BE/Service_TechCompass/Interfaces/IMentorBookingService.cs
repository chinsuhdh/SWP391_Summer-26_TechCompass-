using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Service_TechCompass.DTOs;

namespace Service_TechCompass.Interfaces
{
    public interface IMentorBookingService
    {
        Task<List<MentorDto>> GetMentorsAsync(MentorFilterDto filter);
        Task<Guid> BookMentorAsync(Guid studentId, BookMentorRequestDto request);
        Task UpdatePaymentStatusAsync(Guid sessionId, string paymentStatus);
        Task ApproveBookingAsync(Guid sessionId, string meetingLink);
        Task ReviewNoteAsync(Guid sessionId, string reviewNote);
        Task SendMessageAsync(Guid sessionId, ChatMessageRequestDto request);
    }
}