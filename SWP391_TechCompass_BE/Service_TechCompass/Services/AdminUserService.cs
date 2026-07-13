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
                    CreatedAt = u.CreatedAt
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

                return (200, "Thành công", data);
            }

            // 3. TẠO NGƯỜI DÙNG MỚI (GỘP CHUNG TẤT CẢ ROLE)
            public async Task<(int StatusCode, string Message)> CreateUserAsync(AdminCreateUserDto request)
            {
                try
                {
                    // Kiểm tra Email đã tồn tại chưa
                    if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                    {
                        return (400, "Email này đã tồn tại trong hệ thống.");
                    }

                    // A. Luôn luôn tạo User ở bảng cha (Users) trước
                    var newUserId = Guid.NewGuid();
                    var newUser = new User
                    {
                        UserId = newUserId,
                        Email = request.Email,
                        // Hash mật khẩu trước khi lưu
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),

                        RoleId = request.RoleId,
                        IsActive = request.IsActive,
                        CreatedAt = DateTime.UtcNow,
                        Provider = "Local"
                    };

                    await _context.Users.AddAsync(newUser);

                    // B. Rẽ nhánh tạo dữ liệu cho bảng con tùy theo Role
                    if (request.RoleId == 2) // BỔ SUNG: Tạo STUDENT
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
                    else if (request.RoleId == 3) // Tạo MENTOR
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
                    else if (request.RoleId == 4) // Tạo COUNSELOR
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
                    // (Role 1 - Admin thì chỉ cần tạo bản ghi User là đủ)

                    // C. Lưu tất cả vào Database cùng một lúc
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

                user.RoleId = request.RoleId;
                user.IsActive = request.IsActive;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return (200, "Cập nhật thành công");
            }

        // 5. XÓA NGƯỜI DÙNG
        // 5. XÓA NGƯỜI DÙNG
        public async Task<(int StatusCode, string Message)> DeleteUserAsync(Guid userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return (404, "Không tìm thấy người dùng");

                // 1. TÌM VÀ XÓA DỮ LIỆU Ở CÁC BẢNG CON TRƯỚC (NẾU CÓ)
                var studentRecord = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
                if (studentRecord != null) _context.Students.Remove(studentRecord);

                var mentorRecord = await _context.Mentors.FirstOrDefaultAsync(m => m.UserId == userId);
                if (mentorRecord != null) _context.Mentors.Remove(mentorRecord);

                var counselorRecord = await _context.Counselors.FirstOrDefaultAsync(c => c.UserId == userId);
                if (counselorRecord != null) _context.Counselors.Remove(counselorRecord);

                // Lưu thay đổi xóa bảng con
                await _context.SaveChangesAsync();

                // 2. SAU ĐÓ MỚI XÓA BẢNG CHA (USERS)
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return (200, "Xóa thành công");
            }
            catch (Exception ex)
            {
                // Bọc Try-Catch để nếu có lỗi, Frontend sẽ nhận được tin báo chi tiết thay vì sập ngầm
                return (500, $"Lỗi hệ thống khi xóa: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
    }
    }