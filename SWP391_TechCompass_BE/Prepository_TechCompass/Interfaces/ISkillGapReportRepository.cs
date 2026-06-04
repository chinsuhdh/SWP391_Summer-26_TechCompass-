using Repository_TechCompass.Models;

namespace Repository_TechCompass.Interfaces
{
    public interface ISkillGapReportRepository
    {
        // Lấy toàn bộ kỹ năng yêu cầu của Role và các kỹ năng sinh viên đã pass
        Task<Student?> GetStudentWithSkillsAndTargetAsync(Guid studentId);
        Task<SkillGapReport> SaveReportAsync(SkillGapReport report);
    }
}