using Service_TechCompass.DTOs;

namespace Service_TechCompass.Interfaces
{
    public interface IAiTalentService
    {
        Task<TalentAnalysisDto> GenerateLatentTalentAsync(Guid studentId);
        Task<TalentAnalysisDto> GetTalentAnalysisAsync(Guid studentId);

        Task GenerateLatentTalentForAllStudentsAsync();
    }
}