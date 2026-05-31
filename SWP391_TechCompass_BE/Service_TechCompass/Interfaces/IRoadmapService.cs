using Service_TechCompass.DTOs;

namespace Service_TechCompass.Interfaces
{
    public interface IRoadmapService
    {
        // Nhóm tính năng Dashboard
        Task<(int StatusCode, string Message, DashboardSummaryDto? Data)> GetStudentDashboardAsync(Guid userId);

        // Nhóm tính năng Tech Path Map
        Task<(int StatusCode, string Message, List<SkillNodeDto>? Data)> GetSkillTreeAsync(Guid userId);
        Task<(int StatusCode, string Message)> MarkNodeCompletedAsync(Guid userId, MarkNodeCompletedDto request);
    }
}