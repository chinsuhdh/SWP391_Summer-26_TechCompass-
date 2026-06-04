using Microsoft.EntityFrameworkCore;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;

namespace Repository_TechCompass.Repositories
{
    public class StudentRepository : IStudentRepository
    {
        private readonly Swp391CareerRoadmapContext _context;

        public StudentRepository(Swp391CareerRoadmapContext context)
        {
            _context = context;
        }

        public async Task<Student?> GetStudentByIdAsync(Guid studentId)
        {
            return await _context.Students.FindAsync(studentId);
        }

        public async Task<Student?> GetStudentWithAssessmentsAsync(Guid studentId)
        {
            return await _context.Students
                .Include(s => s.SkillAssessments)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);
        }

        public async Task UpdateStudentAsync(Student student)
        {
            _context.Students.Update(student);
            await _context.SaveChangesAsync();
        }
    }
}