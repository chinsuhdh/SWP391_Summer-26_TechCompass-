using Microsoft.EntityFrameworkCore;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repository_TechCompass.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly Swp391CareerRoadmapContext _context;

        public UserRepository(Swp391CareerRoadmapContext context)
        {
            _context = context;
        }

        // --- TRIỂN KHAI CÁC HÀM ASYNC MỚI ---
        public async Task<User?> GetUserByIdAsync(Guid userId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<Student?> GetStudentByUserIdAsync(Guid userId)
        {
            return await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
        }

        public async Task<Mentor?> GetMentorByUserIdAsync(Guid userId)
        {
            return await _context.Mentors.FirstOrDefaultAsync(m => m.UserId == userId);
        }

        public async Task<Counselor?> GetCounselorByUserIdAsync(Guid userId)
        {
            return await _context.Counselors.FirstOrDefaultAsync(c => c.UserId == userId);
        }

        // --- Giữ nguyên các hàm đồng bộ cũ phục vụ luồng Auth cũ ---
        public User? GetUserById(Guid userId) => _context.Users.FirstOrDefault(u => u.UserId == userId);
        public Student? GetStudentByUserId(Guid userId) => _context.Students.FirstOrDefault(s => s.UserId == userId);
        public Mentor? GetMentorByUserId(Guid userId) => _context.Mentors.FirstOrDefault(m => m.UserId == userId);
        public Counselor? GetCounselorByUserId(Guid userId) => _context.Counselors.FirstOrDefault(c => c.UserId == userId);
        public List<User> GetAllUsers() => _context.Users.ToList();
        public void UpdateUser(User user) => _context.Users.Update(user);
        public void DeleteUser(User user) => _context.Users.Remove(user);
        public List<AiRecommendation> GetAllAiRecommendations() => _context.AiRecommendations.ToList();
        public List<Guid> GetAllUserIds() => _context.Users.Select(u => u.UserId).ToList();
        public List<Role> GetAllRoles() => _context.Roles.ToList();
        public Role? GetRoleById(int roleId) => _context.Roles.FirstOrDefault(r => r.RoleId == roleId);
        public void AddRole(Role role) => _context.Roles.Add(role);
        public void UpdateRole(Role role) => _context.Roles.Update(role);
        public void DeleteRole(Role role) => _context.Roles.Remove(role);
        public void UpdateStudent(Student student) => _context.Students.Update(student);
        public User GetUserByEmail(string email) => _context.Users.FirstOrDefault(u => u.Email == email)!;
        public bool EmailExists(string email) => _context.Users.Any(u => u.Email == email);
        public void AddUser(User user) => _context.Users.Add(user);
        public void AddStudent(Student student) => _context.Students.Add(student);
        public void SaveChanges() => _context.SaveChanges();
        public void AddMentor(Mentor mentor)
        {
            _context.Mentors.Add(mentor);
        }

        public bool IsStudentCodeExists(string studentCode, Guid currentStudentId)
        {
            return _context.Students.Any(s =>
                s.StudentCode == studentCode &&
                s.StudentId != currentStudentId);
        }

        public void AddCounselor(Counselor counselor)
        {
            _context.Counselors.Add(counselor);
        }
    }
}