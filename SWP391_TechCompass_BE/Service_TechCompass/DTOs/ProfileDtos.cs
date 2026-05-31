using System.ComponentModel.DataAnnotations;

public class UserProfileDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty; // <-- Thêm = string.Empty;
    public string FullName { get; set; } = string.Empty; // <-- Thêm = string.Empty;
    public string? StudentCode { get; set; }
    public string? LatentTalentSummary { get; set; }
    public int? TargetRoleId { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class UpdateProfileDto
{
    [Required(ErrorMessage = "Họ và tên không được để trống")]
    public string FullName { get; set; } = string.Empty; // <-- Thêm = string.Empty;

    public string? StudentCode { get; set; }
    public string? LatentTalentSummary { get; set; }
    public int? TargetRoleId { get; set; }
}