using Microsoft.EntityFrameworkCore;
using Repository_TechCompass;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs;
using System;
using System.Threading.Tasks;

namespace Service_TechCompass.Services
{
    public class ProfileService : IProfileService
    {
        private readonly Swp391CareerRoadmapContext _context;

        public ProfileService(Swp391CareerRoadmapContext context)
        {
            _context = context;
        }

        public async Task<UserProfileDto> GetProfileMeAsync(Guid userId)
        {
            // 1. Tìm User từ bảng Core (users) và kèm theo dữ liệu bảng roles
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                throw new Exception("Không tìm thấy thông tin tài khoản!");
            }

            // 2. Gán dữ liệu cơ bản chung cho mọi tài khoản (Cả Admin và Student)
            var profileDto = new UserProfileDto
            {
                UserId = user.UserId,
                Email = user.Email,
                RoleId = user.RoleId,
                RoleName = user.Role?.RoleName ?? "Không xác định"
            };

            // 3. Phân nhánh xử lý theo Vai trò
            if (user.RoleId == 1 || user.Role?.RoleName?.ToLower() == "admin")
            {
                // Nếu là ADMIN: Không cần query bảng students, gán thông tin hiển thị mặc định
                profileDto.FullName = "Quản Trị Viên Hệ Thống";
                profileDto.StudentCode = "ADMIN_ROOT";
                profileDto.TargetCareerRole = "System Manager";
            }
            else
            {
                // Nếu không phải Admin: Tiến hành tìm kiếm thông tin mở rộng ở bảng Students
                var student = await _context.Students
                    .Include(s => s.TargetRole) // Join sang bảng target_career_roles
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (student != null)
                {
                    profileDto.FullName = student.FullName;
                    profileDto.StudentCode = student.StudentCode;

                    // Lấy RoleName từ bảng liên kết thay vì lấy ID
                    profileDto.TargetCareerRole = student.TargetRole?.RoleName;
                }
                else
                {
                    profileDto.FullName = "Người dùng mới (Chưa cập nhật hồ sơ)";
                }
            }

            return profileDto;
        }
    }

    // Khai báo hàm trong Interface để Controller có thể gọi được
    public interface IProfileService
    {
        Task<UserProfileDto> GetProfileMeAsync(Guid userId);
    }
}