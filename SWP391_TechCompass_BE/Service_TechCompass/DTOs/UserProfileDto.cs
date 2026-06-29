namespace Service_TechCompass.DTOs
{
    public class UserProfileDto
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;

        // Các trường mở rộng của Student (để dạng nullable ? vì Admin không có)
        public string? FullName { get; set; }
        public string? StudentCode { get; set; }
        public string? TargetCareerRole { get; set; }
    }
}