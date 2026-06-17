using Service_TechCompass.DTOs;

namespace Service_TechCompass.Interfaces
{
    public interface IRoadmapEngineService
    {
        Task<(int StatusCode, string Message, GenerateRoadmapResponseDto? Data)> GenerateRoadmapAsync(Guid userId);

        // 1. Kiểm tra điều kiện tiên quyết của 1 Node
        Task<(int StatusCode, string Message, bool IsValid)> ValidatePrerequisiteAsync(Guid userId, int nodeId);

        // 2. Lưu trữ lộ trình cũ và tạo lại lộ trình mới
        Task<(int StatusCode, string Message, GenerateRoadmapResponseDto? Data)> RecalculateRoadmapAsync(Guid userId);

        // Đồng bộ điểm số từ bài đánh giá sang Tiến độ lộ trình
        Task SyncProgressAfterAssessmentAsync(Guid userId, int skillNodeId, decimal totalQuizScore, decimal totalCodeScore);

        Task<(int StatusCode, string Message, GenerateRoadmapResponseDto? Data)> GenerateAiRoadmapFromSessionAsync(Guid userId, Guid sessionId);
    }
}