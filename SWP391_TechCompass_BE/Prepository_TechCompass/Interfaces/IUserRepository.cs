using Repository_TechCompass.Models;
using System;
using System.Collections.Generic;

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
        User? GetUserById(Guid userId);

        Student? GetStudentByUserId(Guid userId);
        Mentor? GetMentorByUserId(Guid userId);
        Counselor? GetCounselorByUserId(Guid userId);

        void UpdateStudent(Student student);
        User GetUserByEmail(string email);
        bool EmailExists(string email);
        void AddUser(User user);
        void AddStudent(Student student);
        void SaveChanges();
    }
}