// Mở file Repository_TechCompass/Interfaces/IPortfolioRepository.cs
using Repository_TechCompass.Models;

public interface IPortfolioRepository
{
    // ... các hàm cũ ...
    Task<EPortfolio?> GetPortfolioByStudentIdAsync(Guid studentId);
    Task<EPortfolio?> GetPortfolioByUrlAsync(string url);
    Task<EPortfolio> CreatePortfolioAsync(EPortfolio portfolio);
    Task UpdatePortfolioAsync(EPortfolio portfolio);
    Task SaveGithubRepoAsync(GithubRepository repo);
    Task<GithubRepository?> GetGithubRepoByIdAsync(Guid repoId);
    Task UpdateGithubRepoAsync(GithubRepository repo);

    // THÊM DÒNG NÀY VÀO:
    Task<bool> SyncGithubReposAsync(Guid portfolioId, string githubUsername);
}