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
        Task<(int StatusCode, string Message)> CreateUserAsync(AdminCreateUserDto request);
        Task<(int StatusCode, string Message)> UpdateUserAsync(Guid userId, AdminUpdateUserDto request);
        Task<(int StatusCode, string Message)> DeleteUserAsync(Guid userId);

        // Bổ sung thêm hàm tạo tài khoản cho Staff (Mentor/Counselor) ở đây:
        Task<(int StatusCode, string Message)> CreateStaffAccountAsync(AdminCreateAccountDto request);
    }
}