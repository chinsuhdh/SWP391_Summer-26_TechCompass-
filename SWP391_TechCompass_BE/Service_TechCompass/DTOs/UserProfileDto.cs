namespace Service_TechCompass.DTOs
{
    public class UserProfileDto
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;

        // Dấu ? giúp Admin không bị lỗi khi các trường này không có dữ liệu
        public string? StudentCode { get; set; }
        public string? TargetCareerRole { get; set; }
    }
}