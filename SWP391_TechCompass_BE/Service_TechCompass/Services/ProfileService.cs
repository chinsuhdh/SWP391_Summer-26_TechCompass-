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

        // API 1: Lấy thông tin chung (Cả Admin và Student đều dùng)
        public async Task<UserProfileDto> GetProfileMeAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                throw new Exception("Không tìm thấy thông tin tài khoản!");
            }

            var profileDto = new UserProfileDto
            {
                UserId = user.UserId,
                Email = user.Email,
                RoleId = user.RoleId,
                RoleName = user.Role?.RoleName ?? "Không xác định"
            };

            if (user.RoleId == 1 || user.Role?.RoleName?.ToLower() == "admin")
            {
                profileDto.FullName = "Quản Trị Viên Hệ Thống";
                profileDto.StudentCode = "ADMIN_ROOT";
                profileDto.TargetCareerRole = "System Manager";
            }
            else
            {
                var student = await _context.Students
                    .Include(s => s.TargetRole)
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (student != null)
                {
                    profileDto.FullName = student.FullName;
                    profileDto.StudentCode = student.StudentCode;
                    profileDto.TargetCareerRole = student.TargetRole?.RoleName;
                }
                else
                {
                    profileDto.FullName = "Người dùng mới (Chưa cập nhật hồ sơ)";
                }
            }

            return profileDto;
        }

        // API 2: Cập nhật hồ sơ (Chỉ dành cho Student)
        public async Task<bool> UpdateStudentProfileAsync(Guid userId, UpdateStudentProfileDto dto)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null)
            {
                throw new Exception("Tài khoản Admin không có hồ sơ sinh viên để cập nhật!");
            }

            student.FullName = dto.FullName;
            student.StudentCode = dto.StudentCode;
            student.LatentTalentSummary = dto.LatentTalentSummary;
            student.TargetRoleId = dto.TargetRoleId;
            // student.UpdatedAt = DateTime.UtcNow; // Bỏ comment nếu DB của bạn có cột này

            return await _context.SaveChangesAsync() > 0;
        }

        // API 3: Lấy chi tiết hồ sơ sinh viên (Chuyên sâu)
        public async Task<UserStudentProfileDto> GetStudentProfileOnlyAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.Student)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null || user.Student == null)
            {
                throw new Exception("Không tìm thấy hồ sơ sinh viên tương ứng!");
            }

            return new UserStudentProfileDto
            {
                UserId = user.UserId,
                Email = user.Email,
                FullName = user.Student.FullName,
                StudentCode = user.Student.StudentCode,
                LatentTalentSummary = user.Student.LatentTalentSummary,
                TargetRoleId = user.Student.TargetRoleId
            };
        }
    }

    // Giao diện Interface bọc cả 3 hàm mẫu
    public interface IProfileService
    {
        Task<UserProfileDto> GetProfileMeAsync(Guid userId);
        Task<bool> UpdateStudentProfileAsync(Guid userId, UpdateStudentProfileDto dto);
        Task<UserStudentProfileDto> GetStudentProfileOnlyAsync(Guid userId);
    }
}