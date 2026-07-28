using Repository_TechCompass.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository_TechCompass.Interfaces
{
    public interface IUserRepository
    {
        List<User> GetAllUsers();
        List<Role> GetAllRoles();
        List<AiRecommendation> GetAllAiRecommendations();
        Role? GetRoleById(int roleId);
        void UpdateUser(User user);
        void DeleteUser(User user);
        void AddRole(Role role);
        void UpdateRole(Role role);
        void DeleteRole(Role role);

        // --- CÁC HÀM ĐƯỢC CHUYỂN ĐỔI SANG ASYNC ---
        Task<User?> GetUserByIdAsync(Guid userId);
        Task<Student?> GetStudentByUserIdAsync(Guid userId);
        Task<Mentor?> GetMentorByUserIdAsync(Guid userId);
        Task<Counselor?> GetCounselorByUserIdAsync(Guid userId);
        void AddMentor(Mentor mentor);
        void AddCounselor(Counselor counselor);

        Student? GetStudentByUserId(Guid userId);
        Mentor? GetMentorByUserId(Guid userId);
        Counselor? GetCounselorByUserId(Guid userId);
        User? GetUserById(Guid userId);

        void UpdateStudent(Student student);
        User GetUserByEmail(string email);
        bool EmailExists(string email);
        void AddUser(User user);
        void AddStudent(Student student);

        bool IsStudentCodeExists(string studentCode, Guid currentStudentId);
        void SaveChanges();
    }
}