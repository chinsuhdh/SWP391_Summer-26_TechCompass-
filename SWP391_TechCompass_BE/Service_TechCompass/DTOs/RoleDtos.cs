using System.ComponentModel.DataAnnotations;

namespace Service_TechCompass.DTOs
{
    public class RoleDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }

    public class CreateRoleDto
    {
        [Required(ErrorMessage = "Tên vai trò không được để trống")]
        public string RoleName { get; set; } = string.Empty;
    }

    public class UpdateRoleDto
    {
        [Required(ErrorMessage = "Tên vai trò không được để trống")]
        public string RoleName { get; set; } = string.Empty;
    }
}