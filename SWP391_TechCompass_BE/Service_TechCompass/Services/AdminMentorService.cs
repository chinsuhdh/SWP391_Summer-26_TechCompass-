using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service_TechCompass.Services
{
    public class AdminMentorService : IAdminMentorService
    {
        private readonly IMentorRepository _mentorRepo;
        private readonly IUserRepository _userRepo;

        public AdminMentorService(IMentorRepository mentorRepo, IUserRepository userRepo)
        {
            _mentorRepo = mentorRepo;
            _userRepo = userRepo;
        }

        public Task<(int StatusCode, string Message, List<MentorDto>? Data)> GetAllMentorsAsync()
        {
            var mentors = _mentorRepo.GetAllMentors();
            var data = mentors.Select(m => new MentorDto
            {
                MentorId = m.MentorId,
                UserId = m.UserId,
                Email = m.User?.Email,
                CurrentCompany = m.CurrentCompany,
                ExpertiseTags = m.ExpertiseTags,
                LinkedinUrl = m.LinkedinUrl,
                IsActive = m.User?.IsActive
            }).ToList();

            return Task.FromResult<(int, string, List<MentorDto>?)>((200, "Lấy danh sách Mentor thành công.", data));
        }

        public Task<(int StatusCode, string Message, MentorDto? Data)> GetMentorByIdAsync(Guid mentorId)
        {
            var mentor = _mentorRepo.GetMentorById(mentorId);
            if (mentor == null) return Task.FromResult<(int, string, MentorDto?)>((404, "Không tìm thấy Mentor.", null));

            var data = new MentorDto
            {
                MentorId = mentor.MentorId,
                UserId = mentor.UserId,
                Email = mentor.User?.Email,
                CurrentCompany = mentor.CurrentCompany,
                ExpertiseTags = mentor.ExpertiseTags,
                LinkedinUrl = mentor.LinkedinUrl,
                IsActive = mentor.User?.IsActive
            };

            return Task.FromResult<(int, string, MentorDto?)>((200, "Tìm thấy Mentor.", data));
        }

        public Task<(int StatusCode, string Message)> CreateMentorAsync(CreateMentorDto request)
        {
            if (_userRepo.EmailExists(request.Email))
                return Task.FromResult<(int, string)>((400, "Email này đã tồn tại trong hệ thống."));

            // 1. Tạo User với Role = 3 (Giả định 3 là ID của Mentor Role trong DB)
            var newUser = new User
            {
                UserId = Guid.NewGuid(),
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Provider = "Email",
                RoleId = 3,
                CreatedAt = DateTime.Now,
                IsActive = true // Admin tạo thì kích hoạt luôn
            };
            _userRepo.AddUser(newUser);

            // 2. Tạo Mentor link với User vừa tạo
            var newMentor = new Mentor
            {
                MentorId = Guid.NewGuid(),
                UserId = newUser.UserId,
                CurrentCompany = request.CurrentCompany,
                ExpertiseTags = request.ExpertiseTags,
                LinkedinUrl = request.LinkedinUrl
            };
            _mentorRepo.AddMentor(newMentor);

            _mentorRepo.SaveChanges(); // Lưu cả 2 vào DB
            return Task.FromResult<(int, string)>((201, "Tạo Mentor thành công."));
        }

        public Task<(int StatusCode, string Message)> UpdateMentorAsync(Guid mentorId, UpdateMentorDto request)
        {
            var mentor = _mentorRepo.GetMentorById(mentorId);
            if (mentor == null) return Task.FromResult<(int, string)>((404, "Không tìm thấy Mentor để cập nhật."));

            // Cập nhật thông tin Mentor
            mentor.CurrentCompany = request.CurrentCompany;
            mentor.ExpertiseTags = request.ExpertiseTags;
            mentor.LinkedinUrl = request.LinkedinUrl;

            // Cập nhật trạng thái User (Khóa / Mở khóa)
            if (mentor.User != null)
            {
                mentor.User.IsActive = request.IsActive;
                _userRepo.UpdateUser(mentor.User);
            }

            _mentorRepo.UpdateMentor(mentor);
            _mentorRepo.SaveChanges();

            return Task.FromResult<(int, string)>((200, "Cập nhật Mentor thành công."));
        }

        public Task<(int StatusCode, string Message)> DeleteMentorAsync(Guid mentorId)
        {
            var mentor = _mentorRepo.GetMentorById(mentorId);
            if (mentor == null) return Task.FromResult<(int, string)>((404, "Không tìm thấy Mentor để xóa."));

            var user = mentor.User;

            // Xóa Mentor trước (khóa ngoại)
            _mentorRepo.DeleteMentor(mentor);

            // Xóa luôn User liên kết
            if (user != null) _userRepo.DeleteUser(user);

            _mentorRepo.SaveChanges();
            return Task.FromResult<(int, string)>((200, "Xóa Mentor thành công."));
        }
    }
}