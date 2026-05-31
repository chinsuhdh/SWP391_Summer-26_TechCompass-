using Service_TechCompass.DTOs;

namespace Service_TechCompass.Interfaces
{
    public interface IAdminUserService
    {
        Task<(int StatusCode, string Message, List<AdminUserDetailDto>? Data)> GetAllUsersAsync();
        Task<(int StatusCode, string Message, AdminUserDetailDto? Data)> GetUserByIdAsync(Guid userId);
        Task<(int StatusCode, string Message)> CreateUserAsync(AdminCreateUserDto request);
        Task<(int StatusCode, string Message)> UpdateUserAsync(Guid userId, AdminUpdateUserDto request);
        Task<(int StatusCode, string Message)> DeleteUserAsync(Guid userId);
    }
}