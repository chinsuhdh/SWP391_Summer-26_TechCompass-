using Repository_TechCompass.Models;

namespace Repository_TechCompass.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly Swp391CareerRoadmapContext _context;

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