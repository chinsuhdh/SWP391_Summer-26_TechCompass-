using Service_TechCompass.DTOs;

namespace Service_TechCompass.Interfaces
{
    public interface ISkillGapReportService
    {
        Task<SkillGapReportDto> GenerateGapReportAsync(Guid studentId, string webRootPath);
    }
}