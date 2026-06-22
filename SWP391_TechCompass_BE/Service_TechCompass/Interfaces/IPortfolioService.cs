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

        // BỔ SUNG: Các hàm dành cho Mentor
        Task<List<object>> GetAllPublicPortfoliosAsync();
        Task<PortfolioFeedbackResponseDto> AddPortfolioFeedbackAsync(Guid portfolioId, Guid mentorUserId, CreatePortfolioFeedbackDto dto);
    }
}