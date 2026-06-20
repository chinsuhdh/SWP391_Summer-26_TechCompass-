using Service_TechCompass.DTOs;

namespace Service_TechCompass.Interfaces
{
    public interface ILearningHubService
    {
        // Hàm lấy danh sách tài liệu học tập dựa theo Node ID
        Task<(int StatusCode, string Message, NodeResourcesDto? Data)> GetResourcesByNodeIdAsync(Guid userId, int nodeId);
        Task<(int StatusCode, string Message)> EnrollResourceAsync(Guid userId, int resourceId);

        Task<(int StatusCode, string Message, List<SkillNodeDto>? Data)> GetMyRoadmapAsync(Guid userId);
    }
}