// src/Service_TechCompass/Services/PortfolioService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Octokit;
using Repository_TechCompass;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs;
using Service_TechCompass.Hubs;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class PortfolioService : IPortfolioService
    {
        private readonly IPortfolioRepository _portfolioRepo;
        private readonly IConfiguration _config;
        private readonly Kernel _kernel;
        private readonly ITelemetryService _telemetryService;
        private readonly IHubContext<PortfolioHub> _hubContext;

        public PortfolioService(
            IPortfolioRepository portfolioRepo,
            IConfiguration config,
            Kernel kernel,
            ITelemetryService telemetryService,
            IHubContext<PortfolioHub> hubContext)
        {
            _portfolioRepo = portfolioRepo;
            _config = config;
            _kernel = kernel;
            _telemetryService = telemetryService;
            _hubContext = hubContext;
        }

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

        // FIX DỨT ĐIỂM: Đã xóa bỏ đoạn rác dư thừa lơ lửng do đóng ngoặc sai khi merge conflict
        public async Task<string> GenerateShareableUrlAsync(Guid studentId)
        {
            var p = await _portfolioRepo.GetPortfolioByStudentIdAsync(studentId)
                    ?? await _portfolioRepo.CreatePortfolioAsync(new EPortfolio { PortfolioId = Guid.NewGuid(), StudentId = studentId, CreatedAt = DateTime.Now });

            p.ShareableUrl = $"https://techcompass.com/p/{Guid.NewGuid().ToString("N")[..8]}";
            await _portfolioRepo.UpdatePortfolioAsync(p);
            return p.ShareableUrl;
        }

        // FIX DỨT ĐIỂM: Cập nhật kiểu trả về Task<List<object>> theo đúng Interface mới của bạn Hoa
        public async Task<List<object>> GetAllPublicPortfoliosAsync()
        {
            // TODO: Mentor Phân hệ quản lý - Bạn Hoa sẽ code logic nghiệp vụ ở đây sau
            return await Task.FromResult(new List<object>());
        }

        // FIX DỨT ĐIỂM: Cập nhật kiểu trả về Task<PortfolioFeedbackResponseDto> theo đúng Interface mới của bạn Hoa
        public async Task<PortfolioFeedbackResponseDto> AddPortfolioFeedbackAsync(Guid portfolioId, Guid mentorUserId, CreatePortfolioFeedbackDto dto)
        {
            // TODO: Mentor Phân hệ nhận xét bảng điểm - Bạn Hoa sẽ code logic nghiệp vụ ở đây sau
            return await Task.FromResult(new PortfolioFeedbackResponseDto());
        }

        public async Task<(int StatusCode, string Message)> SyncGithubReposAsync(Guid studentId, string githubUsername)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var portfolio = await _portfolioRepo.GetPortfolioByStudentIdAsync(studentId);
            if (portfolio == null)
            {
                portfolio = await _portfolioRepo.CreatePortfolioAsync(new EPortfolio { PortfolioId = Guid.NewGuid(), StudentId = studentId, CreatedAt = DateTime.Now });
            }

            try
            {
                var github = new GitHubClient(new ProductHeaderValue("TechCompassApp"));
                var githubToken = _config["GithubConfig:PersonalAccessToken"];
                if (!string.IsNullOrEmpty(githubToken)) github.Credentials = new Credentials(githubToken);

                var repos = await github.Repository.GetAllForUser(githubUsername);
                int syncCount = 0;

                var existingRepos = portfolio.GithubRepositories ?? new List<GithubRepository>();

                foreach (var repo in repos)
                {
                    if (repo.Fork || repo.Size == 0 || repo.Archived) continue;
                    string readmeContent = string.Empty;
                    try
                    {
                        var readme = await github.Repository.Content.GetReadme(repo.Id);
                        readmeContent = readme.Content;
                    }
                    catch (NotFoundException) { continue; }

                    if (string.IsNullOrWhiteSpace(readmeContent) || readmeContent.Length < 50) continue;

                    var dbRepo = existingRepos.FirstOrDefault(r => r.GithubUrl == repo.HtmlUrl);

                    if (dbRepo != null)
                    {
                        dbRepo.ReadmeContent = readmeContent;
                        dbRepo.SyncedAt = DateTime.Now;
                        dbRepo.AiProjectSummary = null;
                        dbRepo.ExtractedTechStack = null;

                        await _portfolioRepo.UpdateGithubRepoAsync(dbRepo);
                        syncCount++;
                    }
                    else
                    {
                        var newGithubRepo = new GithubRepository
                        {
                            RepoId = Guid.NewGuid(),
                            PortfolioId = portfolio.PortfolioId,
                            RepoName = repo.Name,
                            GithubUrl = repo.HtmlUrl,
                            ReadmeContent = readmeContent,
                            SyncedAt = DateTime.Now
                        };
                        await _portfolioRepo.SaveGithubRepoAsync(newGithubRepo);
                        syncCount++;
                    }
                }

                watch.Stop();

                await _telemetryService.LogLearningHistoryAsync(
                    studentId: studentId,
                    progressId: null,
                    actionType: "SYNC_GITHUB_SUCCESS",
                    durationSeconds: (int)watch.Elapsed.TotalSeconds,
                    details: $"Đồng bộ thành công {syncCount} repos từ tài khoản GitHub: {githubUsername}"
                );

                await _hubContext.Clients.All.SendAsync("SyncCompleted", studentId.ToString());

                return (200, $"Đồng bộ thành công {syncCount} dự án chất lượng từ GitHub.");
            }
            catch (Exception ex)
            {
                await _telemetryService.LogLearningHistoryAsync(studentId, null, "SYNC_GITHUB_FAILED", 0, ex.Message);
                return (500, $"Lỗi khi kết nối GitHub: {ex.Message}");
            }
        }

        public async Task<(int StatusCode, string Message)> AnalyzeRepoWithAiAsync(Guid repoId)
        {
            var repo = await _portfolioRepo.GetGithubRepoByIdAsync(repoId);
            if (repo == null) return (404, "Không tìm thấy repository.");
            if (string.IsNullOrWhiteSpace(repo.ReadmeContent)) return (400, "Repository này không có file README.md hoặc nội dung quá ngắn.");

            string prompt = $@"Bạn là một chuyên gia tuyển dụng IT. Dưới đây là nội dung file README.md của một dự án:
{repo.ReadmeContent}
Hãy phân tích và trả về đúng định dạng sau. 
QUAN TRỌNG: KHÔNG sử dụng Markdown, KHÔNG in đậm, in nghiêng, KHÔNG thêm dấu sao (**), KHÔNG giải thích thêm:
SUMMARY: [Viết tóm tắt ngắn gọn mục đích dự án trong 2-3 câu]
TECHSTACK: [Liệt kê các công nghệ, framework, ngôn ngữ được sử dụng, phân cách bằng dấu phẩy]";

            var geminiApiKeys = _config.GetSection("GeminiApiConfig:ApiKeys").Get<string[]>() ?? Array.Empty<string>();
            var geminiModelId = _config["GeminiApiConfig:ModelId"] ?? "gemini-2.5-flash";

            var shuffledKeys = geminiApiKeys.OrderBy(_ => Guid.NewGuid()).ToList();

            Exception? lastException = null;
            string aiText = string.Empty;

            foreach (var currentKey in shuffledKeys)
            {
                try
                {
                    IChatCompletionService chatService;

                    if (shuffledKeys.Count == 0)
                    {
                        chatService = _kernel.GetRequiredService<IChatCompletionService>("GeminiChat");
                    }
                    else
                    {
                        var temporaryKernel = Kernel.CreateBuilder()
                            .AddGoogleAIGeminiChatCompletion(modelId: geminiModelId, apiKey: currentKey)
                            .Build();
                        chatService = temporaryKernel.GetRequiredService<IChatCompletionService>();
                    }

                    var result = await chatService.GetChatMessageContentAsync(prompt);
                    aiText = result.Content ?? "";

                    if (!string.IsNullOrWhiteSpace(aiText))
                    {
                        lastException = null;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[GEMINI KEY WARNING]: Một Key bị lỗi ({ex.Message}). Hệ thống đang tự động đổi sang Key dự phòng kế tiếp...");
                    Console.ResetColor();
                }
            }

            if (lastException != null || string.IsNullOrWhiteSpace(aiText))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[AI KERNEL GEMINI CRITICAL ERROR]: Tất cả các API Key đều thất bại. Lỗi cuối cùng: {lastException?.Message}");
                Console.ResetColor();
                return (503, $"Dịch vụ Gemini hiện tại không khả dụng (503) hoặc toàn bộ API Key bị từ chối. Chi tiết: {lastException?.Message}");
            }

            try
            {
                Console.WriteLine("\n[AI RESPONSE DUMP - GEMINI SUCCESS]:\n" + aiText + "\n");

                string cleanText = aiText.Replace("**", "").Replace("Summary:", "SUMMARY:").Replace("Techstack:", "TECHSTACK:").Trim();

                if (cleanText.Contains("SUMMARY:") && cleanText.Contains("TECHSTACK:"))
                {
                    var parts = cleanText.Split(new[] { "TECHSTACK:" }, StringSplitOptions.None);

                    string summaryPart = parts[0].Replace("SUMMARY:", "").Trim();
                    string techStackPart = parts.Length > 1 ? parts[1].Trim() : "";

                    repo.AiProjectSummary = summaryPart;
                    repo.ExtractedTechStack = techStackPart;

                    await _portfolioRepo.UpdateGithubRepoAsync(repo);

                    await _telemetryService.LogLearningHistoryAsync(
                        studentId: Guid.Empty,
                        progressId: null,
                        actionType: "AI_REPO_ANALYZED",
                        durationSeconds: 0,
                        details: $"AI đã phân tích xong repo bằng Gemini thành công."
                    );

                    Console.WriteLine($"[SUCCESS] Đã lưu phân tích Gemini cho repo: {repo.RepoName}");

                    await _hubContext.Clients.All.SendAsync("AnalysisCompleted");

                    return (200, "AI phân tích Repository thành công.");
                }

                return (500, $"AI trả về sai định dạng. Kết quả thực tế: {aiText}");
            }
            catch (Exception ex)
            {
                return (500, $"Lỗi xử lý dữ liệu sau khi gọi AI: {ex.Message}");
            }
        }

        private static PortfolioDto MapToDto(EPortfolio? entity)
        {
            if (entity == null) return new PortfolioDto();
            return new PortfolioDto
            {
                PortfolioId = entity.PortfolioId,
                StudentId = entity.StudentId,
                AiProfileSummary = !string.IsNullOrWhiteSpace(entity.AiProfileSummary)
                                    ? entity.AiProfileSummary
                                    : (entity.Student?.LatentTalentSummary ?? string.Empty),
                ShareableUrl = entity.ShareableUrl,
                CreatedAt = entity.CreatedAt,
                Repositories = (entity.GithubRepositories ?? new List<GithubRepository>())
                    .Select(r => new GithubRepoDto
                    {
                        RepoId = r.RepoId,
                        RepoName = r.RepoName,
                        GithubUrl = r.GithubUrl,
                        ExtractedTechStack = r.ExtractedTechStack ?? string.Empty,
                        AiProjectSummary = r.AiProjectSummary ?? string.Empty,
                        SyncedAt = r.SyncedAt
                    }).ToList()
            };
        }
    }
}