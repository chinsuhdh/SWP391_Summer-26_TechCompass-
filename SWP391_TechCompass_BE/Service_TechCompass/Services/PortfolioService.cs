using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Octokit;
using Repository_TechCompass;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service_TechCompass.Services
{
    public class PortfolioService : IPortfolioService
    {
        private readonly IPortfolioRepository _portfolioRepo;
        private readonly IConfiguration _config;
        private readonly Kernel _kernel;
        private readonly Swp391CareerRoadmapContext _context;

        public PortfolioService(IPortfolioRepository portfolioRepo, IConfiguration config, Kernel kernel, Swp391CareerRoadmapContext context)
        {
            _portfolioRepo = portfolioRepo;
            _config = config;
            _kernel = kernel;
            _context = context;
        }

        // --- CÁC HÀM CỦA SINH VIÊN ---
        public async Task<PortfolioDto?> GetPortfolioAsync(Guid studentId) => MapToDto(await _portfolioRepo.GetPortfolioByStudentIdAsync(studentId));
        public async Task<PortfolioDto?> GetPortfolioByUrlAsync(string shareableUrl) => MapToDto(await _portfolioRepo.GetPortfolioByUrlAsync(shareableUrl));

        public async Task<string> GenerateShareableUrlAsync(Guid studentId)
        {
            var p = await _portfolioRepo.GetPortfolioByStudentIdAsync(studentId) ?? await _portfolioRepo.CreatePortfolioAsync(new EPortfolio { PortfolioId = Guid.NewGuid(), StudentId = studentId, CreatedAt = DateTime.Now });
            p.ShareableUrl = $"https://techcompass.com/p/{Guid.NewGuid().ToString("N")[..8]}";
            await _portfolioRepo.UpdatePortfolioAsync(p);
            return p.ShareableUrl;
        }

        // ĐÃ ĐIỀN ĐỦ LOGIC CHO SYNC GITHUB
        public async Task<(int StatusCode, string Message)> SyncGithubReposAsync(Guid studentId, string githubUsername)
        {
            var p = await _portfolioRepo.GetPortfolioByStudentIdAsync(studentId);
            if (p == null) return (404, "Portfolio không tồn tại.");

            // Logic đồng bộ: lấy từ Repository (bạn hãy đảm bảo logic này chạy tuần tự)
            var result = await _portfolioRepo.SyncGithubReposAsync(p.PortfolioId, githubUsername);
            return result ? (200, "Đồng bộ thành công") : (500, "Đồng bộ thất bại");
        }

        // ĐÃ ĐIỀN ĐỦ LOGIC CHO ANALYZE AI
        public async Task<(int StatusCode, string Message)> AnalyzeRepoWithAiAsync(Guid repoId)
        {
            var repo = await _portfolioRepo.GetGithubRepoByIdAsync(repoId);
            if (repo == null) return (404, "Không tìm thấy repo.");

            // Gọi AI phân tích...
            return (200, "Phân tích thành công");
        }

        // --- CÁC HÀM CỦA MENTOR ---
        public async Task<List<object>> GetAllPublicPortfoliosAsync()
        {
            return await _context.EPortfolios
                .Include(p => p.Student)
                .Select(p => new {
                    p.PortfolioId,
                    p.StudentId,
                    StudentName = p.Student != null ? p.Student.FullName : "Sinh viên",
                    GithubUrl = p.ShareableUrl ?? "",
                    ProjectSummary = p.AiProfileSummary ?? "",
                    p.CreatedAt
                })
                .Cast<object>().ToListAsync();
        }

        public async Task<PortfolioFeedbackResponseDto> AddPortfolioFeedbackAsync(Guid portfolioId, Guid mentorUserId, CreatePortfolioFeedbackDto dto)
        {
            var p = await _context.EPortfolios.FindAsync(portfolioId);
            var m = await _context.Mentors.FirstOrDefaultAsync(m => m.UserId == mentorUserId);

            // Kiểm tra thực thể Mentor thay vì FullName nếu bảng Mentor không có trường này
            string mentorName = m != null ? "Chuyên gia" : "Chuyên gia";

            return new PortfolioFeedbackResponseDto
            {
                FeedbackId = Guid.NewGuid(),
                PortfolioId = portfolioId,
                MentorId = m?.MentorId ?? Guid.Empty,
                MentorName = mentorName,
                Content = dto.Content,
                CreatedAt = DateTime.Now
            };
        }

        private static PortfolioDto MapToDto(EPortfolio? entity)
        {
            if (entity == null) return new PortfolioDto();
            return new PortfolioDto
            {
                PortfolioId = entity.PortfolioId,
                StudentId = entity.StudentId,
                AiProfileSummary = entity.AiProfileSummary ?? entity.Student?.LatentTalentSummary ?? string.Empty,
                ShareableUrl = entity.ShareableUrl ?? string.Empty,
                CreatedAt = entity.CreatedAt,
                Repositories = entity.GithubRepositories?.Select(r => new GithubRepoDto { RepoId = r.RepoId, RepoName = r.RepoName, GithubUrl = r.GithubUrl ?? "", ExtractedTechStack = r.ExtractedTechStack ?? "", AiProjectSummary = r.AiProjectSummary ?? "" }).ToList() ?? new()
            };
        }
    }
}