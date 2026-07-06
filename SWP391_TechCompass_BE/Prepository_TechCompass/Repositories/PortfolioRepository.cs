using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;

namespace Repository_TechCompass.Repositories
{
    public class PortfolioRepository : IPortfolioRepository
    {
        private readonly Swp391CareerRoadmapContext _context;

        public PortfolioRepository(Swp391CareerRoadmapContext context)
        {
            _context = context;
        }
        public async Task<bool> SyncGithubReposAsync(Guid portfolioId, string githubUsername)
        {
            // Chuyển toàn bộ logic đồng bộ GitHub từ Service sang đây (để tách tầng Repository)
            // Hoặc nếu bạn muốn để ở Service thì sửa lại Interface cho khớp.
            // Cách tốt nhất là để ở đây để đúng kiến trúc Repository Pattern.
            return true;
        }
        public async Task<EPortfolio?> GetPortfolioByStudentIdAsync(Guid studentId)
        {
            return await _context.EPortfolios
                .Include(p => p.GithubRepositories)
                .Include(p => p.Student) 
                .FirstOrDefaultAsync(p => p.StudentId == studentId);
        }

        public async Task<EPortfolio?> GetPortfolioByUrlAsync(string url)
        {
            return await _context.EPortfolios
                .Include(p => p.GithubRepositories)
                .Include(p => p.Student)
                .FirstOrDefaultAsync(p => p.ShareableUrl == url);
        }

        public async Task<EPortfolio> CreatePortfolioAsync(EPortfolio portfolio)
        {
            _context.EPortfolios.Add(portfolio);
            await _context.SaveChangesAsync();
            return portfolio;
        }

        public async Task UpdatePortfolioAsync(EPortfolio portfolio)
        {
            _context.EPortfolios.Update(portfolio);
            await _context.SaveChangesAsync();
        }

        public async Task SaveGithubRepoAsync(GithubRepository repo)
        {
            var existing = await _context.GithubRepositories.FindAsync(repo.RepoId);
            if (existing == null)
            {
                _context.GithubRepositories.Add(repo);
            }
            else
            {
                existing.RepoName = repo.RepoName;
                existing.GithubUrl = repo.GithubUrl;
                existing.ReadmeContent = repo.ReadmeContent;
                existing.SyncedAt = repo.SyncedAt;
            }
            await _context.SaveChangesAsync();
        }

        public async Task<GithubRepository?> GetGithubRepoByIdAsync(Guid repoId)
        {
            return await _context.GithubRepositories.FindAsync(repoId);
        }

        public async Task UpdateGithubRepoAsync(GithubRepository repo)
        {
            _context.GithubRepositories.Update(repo);
            await _context.SaveChangesAsync();
        }


    }
}