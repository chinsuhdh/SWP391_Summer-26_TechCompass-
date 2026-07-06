using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service_TechCompass.Services
{
    public class AdminUserService : IAdminUserService
    {
        private readonly IUserRepository _userRepo;

        public AdminUserService(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        // --- CÁC HÀM CŨ CỦA BẠN GIỮ NGUYÊN ---
        public async Task<(int StatusCode, string Message, List<AdminUserDetailDto>? Data)> GetAllUsersAsync()
        {
            // Code cũ của bạn...
            throw new NotImplementedException();
        }

        public async Task<(int StatusCode, string Message)> CreateUserAsync(AdminCreateUserDto request)
        {
            // Code cũ của bạn...
            throw new NotImplementedException();
        }

        // ... Các hàm GetById, Update, Delete khác ...
        // --- 3 HÀM BỊ THIẾU BẠN CẦN THÊM VÀO ---

        public async Task<(int StatusCode, string Message, AdminUserDetailDto? Data)> GetUserByIdAsync(Guid userId)
        {
            // Tạm thời chưa code thì quăng lỗi chưa làm
            throw new NotImplementedException();
        }

        public async Task<(int StatusCode, string Message)> UpdateUserAsync(Guid userId, AdminUpdateUserDto request)
        {
            // Tạm thời chưa code thì quăng lỗi chưa làm
            throw new NotImplementedException();
        }

        public async Task<(int StatusCode, string Message)> DeleteUserAsync(Guid userId)
        {
            // Tạm thời chưa code thì quăng lỗi chưa làm
            throw new NotImplementedException();
        }

        // --- BỔ SUNG THÊM HÀM MỚI CHO STAFF Ở DƯỚI CÙNG ---
        public async Task<(int StatusCode, string Message)> CreateStaffAccountAsync(AdminCreateAccountDto request)
        {
            // 1. Kiểm tra Email đã tồn tại chưa
            if (_userRepo.EmailExists(request.Email))
            {
                return (400, "Email đã tồn tại trong hệ thống.");
            }

            // 2. Chặn nếu Admin cố tình truyền Role không hợp lệ
            if (request.RoleId != 3 && request.RoleId != 4)
            {
                return (400, "API này chỉ hỗ trợ tạo tài khoản cho Mentor (Role 3) hoặc Counselor (Role 4).");
            }

            // 3. Tạo dữ liệu cho bảng cha (User)
            Guid newUserId = Guid.NewGuid();
            var newUser = new User
            {
                UserId = newUserId,
                Email = request.Email,
                PasswordHash = request.Password, // Thực tế nên Hash mật khẩu
                Provider = "Local",
                RoleId = request.RoleId,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            _userRepo.AddUser(newUser);

            // 4. Tạo dữ liệu cho bảng con (Mentor hoặc Counselor)
            if (request.RoleId == 3) // Mentor
            {
                var newMentor = new Mentor
                {
                    MentorId = Guid.NewGuid(),
                    UserId = newUserId,
                    FullName = request.FullName,
                    CurrentCompany = request.CurrentCompany,
                    ExpertiseTags = request.ExpertiseTags
                };
                _userRepo.AddMentor(newMentor);
            }
            else if (request.RoleId == 4) // Counselor
            {
                var newCounselor = new Counselor
                {
                    CounselorId = Guid.NewGuid(),
                    UserId = newUserId,
                    FullName = request.FullName,
                    Department = request.Department,
                    UpdatedAt = DateTime.Now
                };
                _userRepo.AddCounselor(newCounselor);
            }

            // 5. Lưu toàn bộ xuống DB
            _userRepo.SaveChanges();

            return (201, "Tạo tài khoản Staff thành công!");
        }
    }
}