using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Octokit;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class PortfolioService : IPortfolioService
    {
        private readonly IPortfolioRepository _portfolioRepo;
        private readonly IConfiguration _config;
        private readonly Kernel _kernel; // ĐÃ THAY ĐỔI: Sử dụng Kernel thay vì HttpClient

        // ĐÃ THAY ĐỔI: Inject Kernel vào Constructor
        public PortfolioService(IPortfolioRepository portfolioRepo, IConfiguration config, Kernel kernel)
        {
            _portfolioRepo = portfolioRepo;
            _config = config;
            _kernel = kernel;
        }

        // Task 44: Xem E-Portfolio
        public async Task<PortfolioDto?> GetPortfolioAsync(Guid studentId)
        {
            var portfolio = await _portfolioRepo.GetPortfolioByStudentIdAsync(studentId);
            if (portfolio == null) return null;
            return MapToDto(portfolio);
        }

        public async Task<PortfolioDto?> GetPortfolioByUrlAsync(string shareableUrl)
        {
            var portfolio = await _portfolioRepo.GetPortfolioByUrlAsync(shareableUrl);
            if (portfolio == null) return null;
            return MapToDto(portfolio);
        }

        // Task 45: Generate shareable URL
        public async Task<string> GenerateShareableUrlAsync(Guid studentId)
        {
            var portfolio = await _portfolioRepo.GetPortfolioByStudentIdAsync(studentId);
            if (portfolio == null)
            {
                portfolio = new EPortfolio
                {
                    PortfolioId = Guid.NewGuid(),
                    StudentId = studentId,
                    CreatedAt = DateTime.Now
                };
                await _portfolioRepo.CreatePortfolioAsync(portfolio);
            }

            // Tạo URL Unique ngẫu nhiên
            string uniqueSlug = Guid.NewGuid().ToString("N").Substring(0, 8);
            portfolio.ShareableUrl = $"https://techcompass.com/p/{uniqueSlug}";

            await _portfolioRepo.UpdatePortfolioAsync(portfolio);
            return portfolio.ShareableUrl;
        }

        // Task 46, 47, 48: Connect, Sync Repos & Extract README
        public async Task<(int StatusCode, string Message)> SyncGithubReposAsync(Guid studentId, string githubUsername)
        {
            var portfolio = await _portfolioRepo.GetPortfolioByStudentIdAsync(studentId);
            if (portfolio == null)
            {
                portfolio = await _portfolioRepo.CreatePortfolioAsync(new EPortfolio { PortfolioId = Guid.NewGuid(), StudentId = studentId, CreatedAt = DateTime.Now });
            }

            try
            {
                // Khởi tạo Client
                var github = new GitHubClient(new ProductHeaderValue("TechCompassApp"));

                // Nhúng Personal Access Token vào để xé rào Rate Limit lên 5000 req/giờ
                var githubToken = _config["GithubConfig:PersonalAccessToken"];
                if (!string.IsNullOrEmpty(githubToken))
                {
                    github.Credentials = new Credentials(githubToken);
                }

                // Gọi API lấy danh sách Repo
                var repos = await github.Repository.GetAllForUser(githubUsername);

                int syncCount = 0;
                foreach (var repo in repos)
                {
                    // LỌC RÁC TẠI ĐÂY: Bỏ qua repo fork, repo trống rỗng hoặc bị archived
                    if (repo.Fork || repo.Size == 0 || repo.Archived) continue;

                    string readmeContent = string.Empty;
                    try
                    {
                        // Task 48: Extract README.md
                        var readme = await github.Repository.Content.GetReadme(repo.Id);
                        readmeContent = readme.Content;
                    }
                    catch (NotFoundException)
                    {
                        // Bỏ qua hẳn những repo không thèm viết README (không đáng cho vào Portfolio)
                        continue;
                    }

                    // Tránh những repo có README quá ngắn (dưới 50 ký tự - thường là repo rỗng do auto-gen)
                    if (string.IsNullOrWhiteSpace(readmeContent) || readmeContent.Length < 50)
                    {
                        continue;
                    }

                    var githubRepo = new GithubRepository
                    {
                        RepoId = Guid.NewGuid(),
                        PortfolioId = portfolio.PortfolioId,
                        RepoName = repo.Name,
                        GithubUrl = repo.HtmlUrl,
                        ReadmeContent = readmeContent,
                        SyncedAt = DateTime.Now
                    };

                    await _portfolioRepo.SaveGithubRepoAsync(githubRepo);
                    syncCount++;
                }

                return (200, $"Đồng bộ thành công {syncCount} dự án chất lượng từ GitHub.");
            }
            catch (RateLimitExceededException)
            {
                return (429, "Hệ thống đã đạt giới hạn lấy dữ liệu từ GitHub. Vui lòng kiểm tra lại cấu hình Personal Access Token hoặc thử lại sau 1 giờ.");
            }
            catch (Exception ex)
            {
                return (500, $"Lỗi khi kết nối GitHub: {ex.Message}");
            }
        }

        // Task 49 & 50: Tóm tắt Project & Trích xuất Tech Stack bằng AI
        public async Task<(int StatusCode, string Message)> AnalyzeRepoWithAiAsync(Guid repoId)
        {
            var repo = await _portfolioRepo.GetGithubRepoByIdAsync(repoId);
            if (repo == null) return (404, "Không tìm thấy repository.");
            if (string.IsNullOrWhiteSpace(repo.ReadmeContent)) return (400, "Repository này không có file README.md để AI phân tích.");

            // Chuẩn bị Lời nhắc (Prompt)
            string prompt = $@"Bạn là một chuyên gia tuyển dụng IT. Dưới đây là nội dung file README.md của một dự án:
{repo.ReadmeContent}
Hãy phân tích và trả về đúng định dạng sau (Không giải thích thêm):
SUMMARY: [Viết tóm tắt ngắn gọn mục đích dự án trong 2-3 câu]
TECHSTACK: [Liệt kê các công nghệ, framework, ngôn ngữ được sử dụng, phân cách bằng dấu phẩy]";

            try
            {
                // ĐÃ THAY ĐỔI: Sử dụng sức mạnh của Semantic Kernel để gọi Chat AI
                var chatService = _kernel.GetRequiredService<IChatCompletionService>();

                // Gọi API Gemini 2.5 Flash ngầm định thông qua Kernel
                var result = await chatService.GetChatMessageContentAsync(prompt);
                string aiText = result.Content ?? "";

                // Parse kết quả trả về
                if (aiText.Contains("SUMMARY:") && aiText.Contains("TECHSTACK:"))
                {
                    var parts = aiText.Split(new[] { "TECHSTACK:" }, StringSplitOptions.None);
                    repo.AiProjectSummary = parts[0].Replace("SUMMARY:", "").Trim();
                    repo.ExtractedTechStack = parts.Length > 1 ? parts[1].Trim() : "";

                    await _portfolioRepo.UpdateGithubRepoAsync(repo);
                    return (200, "AI phân tích Repository thành công.");
                }

                return (500, "AI trả về sai định dạng mong muốn. Hãy thử phân tích lại.");
            }
            catch (Exception ex)
            {
                return (500, $"Lỗi hệ thống khi gọi AI Semantic Kernel: {ex.Message}");
            }
        }

        private PortfolioDto MapToDto(EPortfolio entity)
        {
            return new PortfolioDto
            {
                PortfolioId = entity.PortfolioId,
                StudentId = entity.StudentId,
                AiProfileSummary = entity.AiProfileSummary,
                ShareableUrl = entity.ShareableUrl,
                CreatedAt = entity.CreatedAt,

                // ĐÃ FIX: Thêm kiểm tra Null cho danh sách GithubRepositories
                Repositories = (entity.GithubRepositories ?? new List<GithubRepository>())
                    .Select(r => new GithubRepoDto
                    {
                        RepoId = r.RepoId,
                        RepoName = r.RepoName,
                        GithubUrl = r.GithubUrl,
                        ExtractedTechStack = r.ExtractedTechStack,
                        AiProjectSummary = r.AiProjectSummary,
                        SyncedAt = r.SyncedAt
                    }).ToList()
            };
        }
    }
}