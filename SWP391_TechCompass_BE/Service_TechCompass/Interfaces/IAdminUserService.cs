using Service_TechCompass.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service_TechCompass.Interfaces
{
    public interface IAdminUserService
    {
        Task<(int StatusCode, string Message, List<AdminUserDetailDto>? Data)> GetAllUsersAsync();
        Task<(int StatusCode, string Message, AdminUserDetailDto? Data)> GetUserByIdAsync(Guid userId);

        // Chỉ dùng 1 hàm Create duy nhất
        Task<(int StatusCode, string Message)> CreateUserAsync(AdminCreateUserDto request);

        Task<(int StatusCode, string Message)> UpdateUserAsync(Guid userId, AdminUpdateUserDto request);
        Task<(int StatusCode, string Message)> DeleteUserAsync(Guid userId);
    }
}