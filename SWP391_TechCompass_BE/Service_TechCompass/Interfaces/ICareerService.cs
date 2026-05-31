using Service_TechCompass.DTOs;

namespace Service_TechCompass.Interfaces
{
    public interface ICareerService
    {
        Task<(int StatusCode, string Message, string? AnalyzedResult)> SubmitSurveyAsync(Guid userId, SubmitSurveyDto request);
        Task<(int StatusCode, string Message)> SelectTargetRoleAsync(Guid userId, SelectTargetRoleDto request);
    }
}