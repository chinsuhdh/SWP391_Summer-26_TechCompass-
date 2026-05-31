using Repository_TechCompass.Models;

namespace Repository_TechCompass.Interfaces // <--- Đã sửa đổi Repositories thành Interfaces
{
    public interface IUserRepository
    {
        List<User> GetAllUsers();
        void DeleteUser(User user);
        List<Role> GetAllRoles();
        Role? GetRoleById(int roleId);
        void AddRole(Role role);
        void UpdateRole(Role role);
        void DeleteRole(Role role);
        User? GetUserById(Guid userId);
        Student? GetStudentByUserId(Guid userId);
        void UpdateStudent(Student student);
        User GetUserByEmail(string email);
        bool EmailExists(string email);
        void AddUser(User user);
        void AddStudent(Student student);
        void SaveChanges();
    }
}