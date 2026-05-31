using Repository_TechCompass.Models;
using Repository_TechCompass.Interfaces;
namespace Repository_TechCompass.Repositories
{
    public class UserRepository : IUserRepository

    {
        public List<User> GetAllUsers()
        {
            return _context.Users.ToList();
        }

        public void DeleteUser(User user)
        {
            _context.Users.Remove(user);
        }
        public List<Role> GetAllRoles()
        {
            return _context.Roles.ToList();
        }

        public Role? GetRoleById(int roleId)
        {
            return _context.Roles.FirstOrDefault(r => r.RoleId == roleId);
        }

        public void AddRole(Role role)
        {
            _context.Roles.Add(role);
        }

        public void UpdateRole(Role role)
        {
            _context.Roles.Update(role);
        }

        public void DeleteRole(Role role)
        {
            _context.Roles.Remove(role);
        }
        private readonly Swp391CareerRoadmapContext _context;

        public User? GetUserById(Guid userId)
        {
            return _context.Users.FirstOrDefault(u => u.UserId == userId);
        }

        public Student? GetStudentByUserId(Guid userId)
        {
            return _context.Students.FirstOrDefault(s => s.UserId == userId);
        }

        public void UpdateStudent(Student student)
        {
            _context.Students.Update(student);
        }

        public UserRepository(Swp391CareerRoadmapContext context)
        {
            _context = context;
        }

        public User GetUserByEmail(string email)
        {
            return _context.Users.FirstOrDefault(u => u.Email == email)!;
        }

        public bool EmailExists(string email)
        {
            return _context.Users.Any(u => u.Email == email);
        }

        public void AddUser(User user)
        {
            _context.Users.Add(user);
        }

        public void AddStudent(Student student)
        {
            _context.Students.Add(student);
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }
    }
}