using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Repository_TechCompass;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;
using System;
using System.Linq;
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

        public async Task<UserProfileDto?> GetProfileMeAsync(Guid userId, int roleId)
        {
            // 1. DÀNH CHO ADMIN
            if (roleId == 1)
            {
                var adminUser = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (adminUser == null) return null;

                return new UserProfileDto
                {
                    UserId = adminUser.UserId,
                    Email = adminUser.Email ?? string.Empty,

                    // SỬA LỖI Ở ĐÂY: Vì bảng User không có tên, ta lấy phần đầu của Email làm tên hoặc để cứng là "Quản trị viên"
                    FullName = adminUser.Email != null ? adminUser.Email.Split('@')[0] : "Quản trị viên",

                    RoleId = adminUser.RoleId,
                    RoleName = adminUser.Role?.RoleName ?? "Admin",
                    StudentCode = "HỆ THỐNG",
                    TargetCareerRole = "Quản lý hệ thống (System Manager)"
                };
            }

            // 2. DÀNH CHO SINH VIÊN
            if (roleId == 2)
            {
                var studentProfile = await _context.Students
                    .Include(s => s.User)
                    .Include(s => s.TargetRole)
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (studentProfile == null) return null;

                return new UserProfileDto
                {
                    UserId = studentProfile.UserId,
                    Email = studentProfile.User?.Email ?? string.Empty,
                    FullName = studentProfile.FullName ?? "Sinh viên",
                    RoleId = studentProfile.User?.RoleId ?? 2,
                    RoleName = "Student",
                    StudentCode = studentProfile.StudentCode,
                    TargetCareerRole = studentProfile.TargetRole?.RoleName
                };
            }

            return null;
        }

        public async Task<bool> UpdateProfileMeAsync(Guid userId, UpdateStudentProfileDto dto)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null) throw new Exception("Hồ sơ sinh viên không tồn tại để cập nhật!");

            student.FullName = dto.FullName;
            student.StudentCode = dto.StudentCode;
            student.LatentTalentSummary = dto.LatentTalentSummary;
            student.TargetRoleId = dto.TargetRoleId;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<string> UploadTranscriptAsync(Guid userId, IFormFile file)
        {
            if (file == null || file.Length == 0) throw new Exception("File không hợp lệ!");
            string fileUrl = $"https://storage.domain.com/transcripts/{userId}_{Guid.NewGuid()}_{file.FileName}";
            return await Task.FromResult(fileUrl);
        }

        public async Task<object> GetTargetRolesAsync()
        {
            return await _context.TargetCareerRoles
                .Select(t => new
                {
                    TargetRoleId = t.TargetRoleId,
                    RoleName = t.RoleName
                })
                .ToListAsync();
        }
    }
}