// Repository_TechCompass/Repositories/PortfolioRepository.cs
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

        // [SECURITY-FIX]: GetStudentByIdAsync — đọc hồ sơ sinh viên để lấy GithubUsername đã đăng ký
        public async Task<Student?> GetStudentByIdAsync(Guid studentId)
        {
            return await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId);
        }

        // [SECURITY-FIX]: UpdateStudentAsync — lưu GithubUsername vào hồ sơ sinh viên lần đầu sync
        public async Task UpdateStudentAsync(Student student)
        {
            _context.Students.Update(student);
            await _context.SaveChangesAsync();
        }

        // [SECURITY-FIX]: DeleteAllReposByPortfolioIdAsync — xóa sạch repos cũ trước khi sync lại
        public async Task DeleteAllReposByPortfolioIdAsync(Guid portfolioId)
        {
            var repos = await _context.GithubRepositories
                .Where(r => r.PortfolioId == portfolioId)
                .ToListAsync();
            if (repos.Any())
            {
                _context.GithubRepositories.RemoveRange(repos);
                await _context.SaveChangesAsync();
            }
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

        // ==========================================
        // THÊM 2 HÀM CÒN THIẾU ĐỂ FIX LỖI INTERFACE
        // ==========================================

        public async Task<EPortfolio?> GetPortfolioByIdAsync(Guid portfolioId)
        {
            // Include thêm các bảng liên quan để AI có context khi phân tích
            return await _context.EPortfolios
                .Include(p => p.GithubRepositories)
                .Include(p => p.Student)
                .FirstOrDefaultAsync(p => p.PortfolioId == portfolioId);
        }

        public async Task<bool> SaveFeedbackSessionAsync(MentorSession session)
        {
            try
            {
                _context.MentorSessions.Add(session);
                var result = await _context.SaveChangesAsync();
                return result > 0; // Trả về true nếu lưu thành công
            }
            catch (Exception)
            {
                return false; // Có thể log exception ở đây nếu cần
            }
        }

        public async Task DeleteGithubRepoAsync(GithubRepository repo)
        {
            _context.GithubRepositories.Remove(repo);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsGithubUsernameTakenAsync(string githubUsername)
        {
            // So sánh không phân biệt hoa thường
            return await _context.Students.AnyAsync(s =>
                s.GithubUsername != null &&
                s.GithubUsername.ToLower() == githubUsername.ToLower());
        }
    }
}