using System;
using System.Threading.Tasks;
using Service_TechCompass.DTOs;

namespace Service_TechCompass.Interfaces
{
    public interface IPortfolioService
    {
        Task<PortfolioDto?> GetPortfolioAsync(Guid studentId);
        Task<PortfolioDto?> GetPortfolioByUrlAsync(string shareableUrl);
        Task<string> GenerateShareableUrlAsync(Guid studentId);
        Task<(int StatusCode, string Message)> SyncGithubReposAsync(Guid studentId, string githubUsername);
        Task<(int StatusCode, string Message)> AnalyzeRepoWithAiAsync(Guid repoId);
    }
}