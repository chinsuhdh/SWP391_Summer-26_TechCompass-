using System;
using System.ComponentModel.DataAnnotations;

namespace Service_TechCompass.DTOs
{
    // 1. DTO dùng để Get danh sách và Xem chi tiết
    public class AdminUserDetailDto
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int RoleId { get; set; }
        public DateTime? CreatedAt { get; set; }

        public string? FullName { get; set; }
        public string? StudentCode { get; set; }
        public string? CurrentCompany { get; set; }
        public string? ExpertiseTags { get; set; }
        public string? Department { get; set; }
    }

    // 2. DTO dùng cho chức năng Tạo mới (Gồm cả Admin, Student, Mentor, Counselor)
    public class AdminCreateUserDto
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu phải từ 6 ký tự")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role ID là bắt buộc")]
        public int RoleId { get; set; } // 1: Admin, 2: Student, 3: Mentor, 4: Counselor

        public bool IsActive { get; set; } = true;

        public string? CurrentCompany { get; set; }
        public string? ExpertiseTags { get; set; }
        public string? Department { get; set; }
    }

    // 3. DTO dùng cho chức năng Cập nhật (Sửa)
    public class AdminUpdateUserDto
    {
        [Required]
        public int RoleId { get; set; }
        public bool IsActive { get; set; }

        public string? FullName { get; set; }
        public string? StudentCode { get; set; }
        public string? CurrentCompany { get; set; }
        public string? ExpertiseTags { get; set; }
        public string? Department { get; set; }
    }
}
