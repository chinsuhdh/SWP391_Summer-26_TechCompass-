using Repository_TechCompass.Models;

namespace Repository_TechCompass.Interfaces
{
    public interface IStudentRepository
    {
        Task<Student?> GetStudentByIdAsync(Guid studentId);
        Task<Student?> GetStudentWithAssessmentsAsync(Guid studentId);
        Task UpdateStudentAsync(Student student);
        Task<List<Student>> GetAllStudentsAsync();
    }
}