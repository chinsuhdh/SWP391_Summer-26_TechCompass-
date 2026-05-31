using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs;

namespace Service_TechCompass.Interfaces
{
    public class AdminUserService : IAdminUserService
    {
        private readonly IUserRepository _userRepo;

        public AdminUserService(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        public Task<(int StatusCode, string Message, List<AdminUserDetailDto>? Data)> GetAllUsersAsync()
        {
            var users = _userRepo.GetAllUsers();
            var data = users.Select(u => new AdminUserDetailDto
            {
                UserId = u.UserId,
                Email = u.Email,
                Provider = u.Provider,
                IsActive = u.IsActive ?? false,
                RoleId = u.RoleId,
                CreatedAt = u.CreatedAt
            }).ToList();

            return Task.FromResult<(int, string, List<AdminUserDetailDto>?)>((200, "Lấy danh sách thành công.", data));
        }

        public Task<(int StatusCode, string Message, AdminUserDetailDto? Data)> GetUserByIdAsync(Guid userId)
        {
            var user = _userRepo.GetUserById(userId);
            if (user == null) return Task.FromResult<(int, string, AdminUserDetailDto?)>((404, "Không tìm thấy người dùng.", null));

            var data = new AdminUserDetailDto
            {
                UserId = user.UserId,
                Email = user.Email,
                Provider = user.Provider,
                IsActive = user.IsActive ?? false,
                RoleId = user.RoleId,
                CreatedAt = user.CreatedAt
            };
            return Task.FromResult<(int, string, AdminUserDetailDto?)>((200, "Tìm thấy người dùng.", data));
        }

        public Task<(int StatusCode, string Message)> CreateUserAsync(AdminCreateUserDto request)
        {
            if (_userRepo.EmailExists(request.Email))
                return Task.FromResult((400, "Email đã tồn tại trên hệ thống."));

            var newUser = new User
            {
                UserId = Guid.NewGuid(),
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Provider = "Email",
                IsActive = request.IsActive,
                CreatedAt = DateTime.Now,
                RoleId = request.RoleId
            };

            _userRepo.AddUser(newUser);
            _userRepo.SaveChanges();
            return Task.FromResult((201, "Admin tạo người dùng mới thành công."));
        }

        public Task<(int StatusCode, string Message)> UpdateUserAsync(Guid userId, AdminUpdateUserDto request)
        {
            var user = _userRepo.GetUserById(userId);
            if (user == null) return Task.FromResult((404, "Không tìm thấy người dùng để cập nhật."));

            user.RoleId = request.RoleId;
            user.IsActive = request.IsActive;

            _userRepo.SaveChanges();
            return Task.FromResult((200, "Cập nhật trạng thái người dùng thành công."));
        }

        public Task<(int StatusCode, string Message)> DeleteUserAsync(Guid userId)
        {
            var user = _userRepo.GetUserById(userId);
            if (user == null) return Task.FromResult((404, "Không tìm thấy người dùng để xóa."));

            _userRepo.DeleteUser(user);
            _userRepo.SaveChanges();
            return Task.FromResult((200, "Xóa người dùng thành công."));
        }
    }
}