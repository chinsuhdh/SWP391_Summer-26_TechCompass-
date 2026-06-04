using Repository_TechCompass.Models;

namespace Repository_TechCompass.Interfaces
{
    public interface IStudentRepository
    {
        Task<Student?> GetStudentByIdAsync(Guid studentId);
        // Lấy thông tin sinh viên kèm theo các bài test kỹ năng để AI có dữ liệu phân tích
        Task<Student?> GetStudentWithAssessmentsAsync(Guid studentId);
        Task UpdateStudentAsync(Student student);
    }
}