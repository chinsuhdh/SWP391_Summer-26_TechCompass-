using System;
using System.ComponentModel.DataAnnotations;

namespace Service_TechCompass.DTOs
{
    public class MentorDto
    {
        public Guid MentorId { get; set; }
        public Guid UserId { get; set; }
        public string? Email { get; set; }
        public string? CurrentCompany { get; set; }
        public string? ExpertiseTags { get; set; }
        public string? LinkedinUrl { get; set; }
        public bool? IsActive { get; set; }
    }

    public class CreateMentorDto
    {
        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        public string Password { get; set; } = string.Empty;

        public string? CurrentCompany { get; set; }
        public string? ExpertiseTags { get; set; }
        public string? LinkedinUrl { get; set; }
    }

    public class UpdateMentorDto
    {
        public string? CurrentCompany { get; set; }
        public string? ExpertiseTags { get; set; }
        public string? LinkedinUrl { get; set; }
        public bool IsActive { get; set; }
    }
}