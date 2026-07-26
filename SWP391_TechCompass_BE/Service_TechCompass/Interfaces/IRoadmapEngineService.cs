using Repository_TechCompass.Models;
using Service_TechCompass.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service_TechCompass.Interfaces
{
    public interface IRoadmapEngineService
    {
        Task<(int StatusCode, string Message, GenerateRoadmapResponseDto? Data)> GenerateRoadmapAsync(Guid userId);

        Task<(int StatusCode, string Message, bool IsValid)> ValidatePrerequisiteAsync(Guid userId, int nodeId);

        Task<(int StatusCode, string Message, GenerateRoadmapResponseDto? Data)> RecalculateRoadmapAsync(Guid userId);

        Task SyncProgressAfterAssessmentAsync(Guid userId, int skillNodeId, decimal totalQuizScore, decimal totalCodeScore);

        // THAY THẾ LUỒNG TẠO ROADMAP BẰNG LUỒNG XỬ LÝ KẾT QUẢ & PHÂN TÍCH AI MENTOR
        Task<(int StatusCode, string Message, object? Data)> ProcessAssessmentResultAsync(Guid userId, Guid sessionId);

        Task SyncPlacementTestProgressAsync(Guid userId, List<AssessmentQuizDetail> quizDetails);
    }
}