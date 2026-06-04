using System;
using System.Threading.Tasks;
using Repository_TechCompass.Models;

namespace Repository_TechCompass.Interfaces
{
    public interface IPortfolioRepository
    {
        Task<EPortfolio?> GetPortfolioByStudentIdAsync(Guid studentId);
        Task<EPortfolio?> GetPortfolioByUrlAsync(string url);
        Task<EPortfolio> CreatePortfolioAsync(EPortfolio portfolio);
        Task UpdatePortfolioAsync(EPortfolio portfolio);
        Task SaveGithubRepoAsync(GithubRepository repo);
        Task<GithubRepository?> GetGithubRepoByIdAsync(Guid repoId);
        Task UpdateGithubRepoAsync(GithubRepository repo);
    }
}