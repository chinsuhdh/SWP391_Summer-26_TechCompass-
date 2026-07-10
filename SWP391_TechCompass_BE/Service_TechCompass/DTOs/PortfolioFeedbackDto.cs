using System;
using System.ComponentModel.DataAnnotations;

namespace Service_TechCompass.DTOs
{
    // DTO nhận dữ liệu bình luận từ Mentor gửi lên
    public class CreatePortfolioFeedbackDto
    {
        [Required(ErrorMessage = "Nội dung nhận xét không được để trống.")]
        [StringLength(2000, ErrorMessage = "Nội dung nhận xét không được vượt quá 2000 ký tự.")]
        public string Content { get; set; } = string.Empty;
    }

    // DTO trả về kết quả sau khi lưu feedback thành công
    public class PortfolioFeedbackResponseDto
    {
        public Guid FeedbackId { get; set; }
        public Guid PortfolioId { get; set; }
        public Guid MentorId { get; set; }
        public string MentorName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    // Thêm DTO này nếu chưa có
    public class StudentFeedbackDto
    {
        public Guid SessionId { get; set; }
        public string MentorName { get; set; } = string.Empty;
        public string MentorCompany { get; set; } = string.Empty;
        public string ReviewNotes { get; set; } = string.Empty;
        public DateTime? ScheduledAt { get; set; }
    }
}