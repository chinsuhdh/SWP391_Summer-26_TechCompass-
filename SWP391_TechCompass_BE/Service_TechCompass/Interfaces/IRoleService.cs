using Service_TechCompass.DTOs;

namespace Service_TechCompass.Interfaces
{
    public interface IRoleService
    {
        Task<(int StatusCode, string Message, List<RoleDto>? Data)> GetAllRolesAsync();
        Task<(int StatusCode, string Message, RoleDto? Data)> GetRoleByIdAsync(int roleId);
        Task<(int StatusCode, string Message)> CreateRoleAsync(CreateRoleDto request);
        Task<(int StatusCode, string Message)> UpdateRoleAsync(int roleId, UpdateRoleDto request);
        Task<(int StatusCode, string Message)> DeleteRoleAsync(int roleId);
    }
}