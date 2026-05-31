using System.ComponentModel.DataAnnotations;

namespace Service_TechCompass.DTOs
{
    public class AdminUserDetailDto
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int RoleId { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class AdminCreateUserDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6, ErrorMessage = "Mật khẩu tối thiểu phải từ 6 ký tự")]
        public string Password { get; set; } = string.Empty;

        [Required]
        public int RoleId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class AdminUpdateUserDto
    {
        [Required]
        public int RoleId { get; set; }
        public bool IsActive { get; set; }
    }
}