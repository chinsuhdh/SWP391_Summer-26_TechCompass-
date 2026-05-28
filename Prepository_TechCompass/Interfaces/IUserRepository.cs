using Repository_TechCompass.Models;

namespace Repository_TechCompass.Repositories
{
    public interface IUserRepository
    {
        User GetUserByEmail(string email);
        bool EmailExists(string email);
        void AddUser(User user);
        void AddStudent(Student student);
        void SaveChanges();
    }
}