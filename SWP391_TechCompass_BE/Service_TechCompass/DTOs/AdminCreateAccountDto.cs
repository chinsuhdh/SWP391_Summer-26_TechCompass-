using System.ComponentModel.DataAnnotations;

namespace Service_TechCompass.DTOs
{
    public class AdminCreateAccountDto
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Role ID là bắt buộc")]
        public int RoleId { get; set; } // 3 là Mentor, 4 là Counselor

        // Các trường tùy chọn (Tùy thuộc vào Role)
        public string? CurrentCompany { get; set; } // Dành cho Mentor
        public string? ExpertiseTags { get; set; } // Dành cho Mentor
        public string? Department { get; set; } // Dành cho Counselor
    }
}