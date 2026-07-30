using Microsoft.EntityFrameworkCore;
using Repository_TechCompass;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service_TechCompass.Services
{
    public class AdminUserService : IAdminUserService
    {
        private readonly Swp391CareerRoadmapContext _context;

        public AdminUserService(Swp391CareerRoadmapContext context)
        {
            _context = context;
        }

        // 1. LẤY DANH SÁCH NGƯỜI DÙNG
        public async Task<(int StatusCode, string Message, List<AdminUserDetailDto>? Data)> GetAllUsersAsync()
        {
            var users = await _context.Users.Select(u => new AdminUserDetailDto
            {
                UserId = u.UserId,
                Email = u.Email ?? "",
                Provider = u.Provider ?? "Local",
                IsActive = u.IsActive ?? true,
                RoleId = u.RoleId,
                CreatedAt = u.CreatedAt,

                // Trích xuất FullName từ các bảng con dựa vào RoleId
                FullName = u.RoleId == 2 ? _context.Students.Where(s => s.UserId == u.UserId).Select(s => s.FullName).FirstOrDefault() :
                           u.RoleId == 3 ? _context.Mentors.Where(m => m.UserId == u.UserId).Select(m => m.FullName).FirstOrDefault() :
                           u.RoleId == 4 ? _context.Counselors.Where(c => c.UserId == u.UserId).Select(c => c.FullName).FirstOrDefault() :
                           "System Admin"
            }).ToListAsync();

            return (200, "Lấy danh sách thành công", users);
        }

        // 2. LẤY CHI TIẾT NGƯỜI DÙNG THEO ID
        public async Task<(int StatusCode, string Message, AdminUserDetailDto? Data)> GetUserByIdAsync(Guid userId)
        {
            var u = await _context.Users.FindAsync(userId);
            if (u == null) return (404, "Không tìm thấy người dùng", null);

            var data = new AdminUserDetailDto
            {
                UserId = u.UserId,
                Email = u.Email ?? "",
                Provider = u.Provider ?? "Local",
                IsActive = u.IsActive ?? true,
                RoleId = u.RoleId,
                CreatedAt = u.CreatedAt
            };

            // Lấy thêm thông tin chi tiết từ bảng con tùy theo Role
            if (u.RoleId == 2) // Student
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
                if (student != null)
                {
                    data.FullName = student.FullName;
                    data.StudentCode = student.StudentCode;
                }
            }
            else if (u.RoleId == 3) // Mentor
            {
                var mentor = await _context.Mentors.FirstOrDefaultAsync(m => m.UserId == userId);
                if (mentor != null)
                {
                    data.FullName = mentor.FullName;
                    data.CurrentCompany = mentor.CurrentCompany;
                    data.ExpertiseTags = mentor.ExpertiseTags;
                }
            }
            else if (u.RoleId == 4) // Counselor
            {
                var counselor = await _context.Counselors.FirstOrDefaultAsync(c => c.UserId == userId);
                if (counselor != null)
                {
                    data.FullName = counselor.FullName;
                    data.Department = counselor.Department;
                }
            }

            return (200, "Thành công", data);
        }

        // 3. TẠO NGƯỜI DÙNG MỚI
        public async Task<(int StatusCode, string Message)> CreateUserAsync(AdminCreateUserDto request)
        {
            try
            {
                if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                {
                    return (400, "Email này đã tồn tại trong hệ thống.");
                }

                var newUserId = Guid.NewGuid();
                var newUser = new User
                {
                    UserId = newUserId,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    RoleId = request.RoleId,
                    IsActive = request.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    Provider = "Local"
                };

                await _context.Users.AddAsync(newUser);

                if (request.RoleId == 2) // STUDENT
                {
                    var newStudent = new Student
                    {
                        StudentId = Guid.NewGuid(),
                        UserId = newUserId,
                        FullName = request.FullName,
                        UpdatedAt = DateTime.UtcNow,
                        StudentCode = "SE" + new Random().Next(100000, 999999).ToString()
                    };
                    await _context.Students.AddAsync(newStudent);
                }
                else if (request.RoleId == 3) // MENTOR
                {
                    var newMentor = new Mentor
                    {
                        MentorId = Guid.NewGuid(),
                        UserId = newUserId,
                        FullName = request.FullName,
                        CurrentCompany = request.CurrentCompany,
                        ExpertiseTags = request.ExpertiseTags
                    };
                    await _context.Mentors.AddAsync(newMentor);
                }
                else if (request.RoleId == 4) // COUNSELOR
                {
                    var newCounselor = new Counselor
                    {
                        CounselorId = Guid.NewGuid(),
                        UserId = newUserId,
                        FullName = request.FullName,
                        Department = request.Department,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _context.Counselors.AddAsync(newCounselor);
                }

                await _context.SaveChangesAsync();
                return (201, "Tạo người dùng thành công!");
            }
            catch (Exception ex)
            {
                return (500, $"Lỗi server: {ex.Message}");
            }
        }

        // 4. CẬP NHẬT NGƯỜI DÙNG
        public async Task<(int StatusCode, string Message)> UpdateUserAsync(Guid userId, AdminUpdateUserDto request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return (404, "Không tìm thấy người dùng");

            if (user.RoleId != request.RoleId)
            {
                return (400, "Không hỗ trợ đổi Role của người dùng trực tiếp. Vui lòng tạo tài khoản mới nếu muốn đổi chức vụ.");
            }

            user.IsActive = request.IsActive;
            _context.Users.Update(user);

            if (user.RoleId == 2) // Student
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
                if (student != null)
                {
                    student.FullName = request.FullName ?? student.FullName;
                    student.StudentCode = request.StudentCode ?? student.StudentCode;
                    student.UpdatedAt = DateTime.UtcNow;
                    _context.Students.Update(student);
                }
            }
            else if (user.RoleId == 3) // Mentor
            {
                var mentor = await _context.Mentors.FirstOrDefaultAsync(m => m.UserId == userId);
                if (mentor != null)
                {
                    mentor.FullName = request.FullName ?? mentor.FullName;
                    mentor.CurrentCompany = request.CurrentCompany ?? mentor.CurrentCompany;
                    mentor.ExpertiseTags = request.ExpertiseTags ?? mentor.ExpertiseTags;
                    _context.Mentors.Update(mentor);
                }
            }
            else if (user.RoleId == 4) // Counselor
            {
                var counselor = await _context.Counselors.FirstOrDefaultAsync(c => c.UserId == userId);
                if (counselor != null)
                {
                    counselor.FullName = request.FullName ?? counselor.FullName;
                    counselor.Department = request.Department ?? counselor.Department;
                    counselor.UpdatedAt = DateTime.UtcNow;
                    _context.Counselors.Update(counselor);
                }
            }

            await _context.SaveChangesAsync();
            return (200, "Cập nhật toàn diện thông tin người dùng thành công");
        }

        // 5. XÓA NGƯỜI DÙNG (Giữ hàm này để tuân thủ IAdminUserService nhưng chuyển thành Soft Delete)
        public async Task<(int StatusCode, string Message)> DeleteUserAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return (404, "Không tìm thấy người dùng");

            // Chuyển từ "Hard Delete" sang "Soft Delete" (Chỉ khóa tài khoản)
            user.IsActive = false;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return (200, "Tài khoản đã được khóa an toàn (Không xóa dữ liệu).");
        }
    }
}