// Service_TechCompass/Interfaces/IAdminContentService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Service_TechCompass.DTOs;

namespace Service_TechCompass.Interfaces
{
    public interface IAdminContentService
    {
        // --- TECH PATH CRUD ---
        Task<(int StatusCode, string Message, List<TechPathDto>? Data)> GetAllTechPathsAsync();
        Task<(int StatusCode, string Message, TechPathDto? Data)> GetTechPathByIdAsync(int id);
        Task<(int StatusCode, string Message)> CreateTechPathAsync(CreateUpdateTechPathDto request);
        Task<(int StatusCode, string Message)> UpdateTechPathAsync(int id, CreateUpdateTechPathDto request);
        Task<(int StatusCode, string Message)> DeleteTechPathAsync(int id);

        // --- SKILL NODE CRUD ---
        Task<(int StatusCode, string Message, List<SkillNodeDto>? Data)> GetAllSkillNodesAsync();
        Task<(int StatusCode, string Message, SkillNodeDto? Data)> GetSkillNodeByIdAsync(int id);
        Task<(int StatusCode, string Message)> CreateSkillNodeAsync(CreateUpdateSkillNodeDto request);
        Task<(int StatusCode, string Message)> UpdateSkillNodeAsync(int id, CreateUpdateSkillNodeDto request);
        Task<(int StatusCode, string Message)> DeleteSkillNodeAsync(int id);

        // --- LEARNING RESOURCE CRUD ---
        // FIX: Đã xóa hàm GetAllLearningResourcesAsync() cũ trả về List để tránh nhập nhằng dữ liệu
        Task<(int StatusCode, string Message, LearningResourceDto? Data)> GetLearningResourceByIdAsync(int id);
        Task<(int StatusCode, string Message)> CreateLearningResourceAsync(CreateUpdateLearningResourceDto request);
        Task<(int StatusCode, string Message)> UpdateLearningResourceAsync(int id, CreateUpdateLearningResourceDto request);
        Task<(int StatusCode, string Message)> DeleteLearningResourceAsync(int id);

        Task<(int StatusCode, string Message)> SyncRoadmapFromGitHubAsync(string rawUrl, int targetRoleId);

        // HÀM CHÍNH THỨC: Hỗ trợ phân trang và lọc tìm kiếm cho hệ thống
        Task<(int StatusCode, string Message, PagedResult<LearningResourceDto>? Data)> GetAllLearningResourcesAsync(int page = 1, int pageSize = 50, int? nodeId = null, string searchTitle = null);
    }
}