using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Service_TechCompass.DTOs;

namespace Service_TechCompass.Interfaces
{
    public interface IPortfolioService
    {
        // Các hàm dành cho sinh viên
        Task<PortfolioDto?> GetPortfolioAsync(Guid studentId);
        Task<PortfolioDto?> GetPortfolioByUrlAsync(string shareableUrl);
        Task<string> GenerateShareableUrlAsync(Guid studentId);
        Task<(int StatusCode, string Message)> SyncGithubReposAsync(Guid studentId, string githubUsername);
        Task<(int StatusCode, string Message)> AnalyzeRepoWithAiAsync(Guid repoId);

        // Các hàm dành cho Mentor
        Task<List<object>> GetAllPublicPortfoliosAsync();
        Task<PortfolioFeedbackResponseDto> AddPortfolioFeedbackAsync(Guid portfolioId, Guid mentorUserId, CreatePortfolioFeedbackDto dto);

        // HÀM MỚI: AI tạo nháp nhận xét cho Mentor
        Task<string> GenerateAiFeedbackSuggestionAsync(Guid portfolioId);

        // Các hàm xử lý AI Summary ngầm bằng Hangfire
        Task GenerateEPortfolioSummaryAsync(Guid studentId, Guid portfolioId);
        Task ProcessFullGithubPipelineAsync(Guid studentId, string githubUsername);

        // Phân tích độ khớp nghề nghiệp (Role Suitability)
        Task EvaluateRoleSuitabilityAsync(Guid studentId, Guid portfolioId);
    }
}