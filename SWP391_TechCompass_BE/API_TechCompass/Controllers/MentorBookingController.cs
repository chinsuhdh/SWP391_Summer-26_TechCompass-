using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;

namespace API_TechCompass.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class MentorBookingController : ControllerBase
    {
        private readonly IMentorBookingService _mentorService;

        public MentorBookingController(IMentorBookingService mentorService)
        {
            _mentorService = mentorService;
        }

        [HttpGet("mentors")]
        public async Task<IActionResult> GetMentors([FromQuery] MentorFilterDto filter)
        {
            var result = await _mentorService.GetMentorsAsync(filter);
            return Ok(new { data = result });
        }

        [HttpPost("book")]
        public async Task<IActionResult> BookMentor([FromQuery] Guid studentId, [FromBody] BookMentorRequestDto request)
        {
            var sessionId = await _mentorService.BookMentorAsync(studentId, request);
            return Ok(new { message = "Gửi yêu cầu đặt lịch thành công", sessionId });
        }

        [HttpPut("{sessionId}/payment")]
        public async Task<IActionResult> UpdatePayment(Guid sessionId, [FromBody] UpdatePaymentStatusDto request)
        {
            await _mentorService.UpdatePaymentStatusAsync(sessionId, request.PaymentStatus);
            return Ok(new { message = "Cập nhật trạng thái thanh toán thành công" });
        }

        [HttpPut("{sessionId}/approve")]
        public async Task<IActionResult> ApproveBooking(Guid sessionId, [FromBody] ApproveBookingDto request)
        {
            await _mentorService.ApproveBookingAsync(sessionId, request.MeetingLink);
            return Ok(new { message = "Đã duyệt lịch hẹn và gửi thông báo cho Sinh viên." });
        }

        [HttpPut("{sessionId}/review")]
        public async Task<IActionResult> ReviewSession(Guid sessionId, [FromBody] ReviewNoteDto request)
        {
            await _mentorService.ReviewNoteAsync(sessionId, request.ReviewNotes);
            return Ok(new { message = "Đã lưu Review Note thành công." });
        }

        [HttpPost("{sessionId}/chat")]
        public async Task<IActionResult> SendChatMessage(Guid sessionId, [FromBody] ChatMessageRequestDto request)
        {
            await _mentorService.SendMessageAsync(sessionId, request);
            return Ok(new { message = "Tin nhắn đã gửi thành công." });
        }
    }
}