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
            Console.WriteLine($"\n[BACKEND-SYNC] Bắt đầu đồng bộ GitHub cho username: '{githubUsername}' (StudentId: {studentId})"); // LOG BẮT ĐẦU

            var portfolio = await _portfolioRepo.GetPortfolioByStudentIdAsync(studentId);
            if (portfolio == null)
            {
                portfolio = await _portfolioRepo.CreatePortfolioAsync(new EPortfolio { PortfolioId = Guid.NewGuid(), StudentId = studentId, CreatedAt = DateTime.Now });
            }

            try
            {
                var github = new GitHubClient(new ProductHeaderValue("TechCompassApp"));
                var githubToken = _config["GithubConfig:PersonalAccessToken"];

                if (string.IsNullOrEmpty(githubToken))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[BACKEND-WARNING] Không tìm thấy GitHub PersonalAccessToken trong config. Hệ thống đang gọi API ẩn danh (Rất dễ bị Rate Limit 60 req/hour).");
                    Console.ResetColor();
                }
                else
                {
                    github.Credentials = new Credentials(githubToken);
                }

                Console.WriteLine("[BACKEND-SYNC] Đang gọi API GitHub kéo danh sách Repositories...");
                var repos = await github.Repository.GetAllForUser(githubUsername);
                Console.WriteLine($"[BACKEND-SYNC] Kéo thành công {repos.Count} repos từ GitHub. Bắt đầu lọc và lưu Database...");

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
                    catch (NotFoundException)
                    {
                        // Không có readme thì bỏ qua repo này
                        continue;
                    }

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
                Console.WriteLine($"[BACKEND-SYNC SUCCESS] Xử lý xong {syncCount} repos hợp lệ. Thời gian: {watch.Elapsed.TotalSeconds}s");

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
            catch (NotFoundException ex)
            {
                // Bắt lỗi nhập sai username GitHub
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[BACKEND-SYNC LỖI] Không tìm thấy tài khoản GitHub '{githubUsername}'. Chi tiết: {ex.Message}");
                Console.ResetColor();
                await _telemetryService.LogLearningHistoryAsync(studentId, null, "SYNC_GITHUB_FAILED", 0, "Username không tồn tại.");
                return (404, $"Không tìm thấy tài khoản GitHub: {githubUsername}");
            }
            catch (RateLimitExceededException ex)
            {
                // Bắt lỗi gọi quá nhiều lần bị GitHub chặn (thường xuyên xảy ra nếu thiếu Token)
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[BACKEND-SYNC LỖI] Bị GitHub API chặn do quá giới hạn Rate Limit. Chi tiết: {ex.Message}");
                Console.ResetColor();
                await _telemetryService.LogLearningHistoryAsync(studentId, null, "SYNC_GITHUB_FAILED", 0, "Lỗi Rate Limit GitHub.");
                return (429, "Hệ thống đang bị giới hạn lượt tải từ GitHub. Vui lòng thử lại sau.");
            }
            catch (Exception ex)
            {
                // Bắt các lỗi Database, Network,...
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[BACKEND-SYNC CRITICAL LỖI] Lỗi hệ thống: {ex.Message}");
                Console.WriteLine($"[STACK TRACE] {ex.StackTrace}"); // In ra dòng code gây lỗi
                Console.ResetColor();

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

        // =========================================================================
        // PHÂN HỆ MỚI: TẠO AI SUMMARY CHO PORTFOLIO & MASTER HANGFIRE PIPELINE
        // =========================================================================

        public async Task GenerateEPortfolioSummaryAsync(Guid studentId, Guid portfolioId)
        {
            var portfolio = await _portfolioRepo.GetPortfolioByStudentIdAsync(studentId);
            if (portfolio == null) return;

            string talentSummary = portfolio.Student?.LatentTalentSummary ?? "Sinh viên đang trong quá trình đánh giá năng lực cơ bản.";

            var analyzedRepos = portfolio.GithubRepositories?
                .Where(r => !string.IsNullOrWhiteSpace(r.AiProjectSummary))
                .ToList() ?? new List<GithubRepository>();

            if (!analyzedRepos.Any()) return;

            string repoSummaries = string.Join("\n", analyzedRepos.Select(r =>
                $"- Dự án {r.RepoName}: {r.AiProjectSummary} (Công nghệ: {r.ExtractedTechStack})"));

            string prompt = $@"Bạn là một chuyên gia nhân sự cấp cao trong ngành IT. Dưới đây là dữ liệu của một ứng viên:

[ĐÁNH GIÁ NĂNG LỰC CỐT LÕI]:
{talentSummary}

[CÁC DỰ ÁN THỰC TẾ]:
{repoSummaries}

Dựa trên dữ liệu này, hãy viết một đoạn 'Professional Summary' (khoảng 150-200 từ) để đặt ở trang chủ E-Portfolio của ứng viên. 
YÊU CẦU:
1. Nhấn mạnh sự kết hợp giữa tư duy nền tảng và khả năng áp dụng công nghệ thực tế.
2. Viết dưới dạng một đoạn văn chuyên nghiệp, truyền cảm hứng.
3. KHÔNG sử dụng Markdown (không in đậm, in nghiêng, gạch đầu dòng). KHÔNG bịa đặt thêm công nghệ.";

            try
            {
                var chatService = _kernel.GetRequiredService<IChatCompletionService>("GeminiChat");
                var result = await chatService.GetChatMessageContentAsync(prompt);

                string aiSummary = result.Content?.Trim() ?? string.Empty;

                if (!string.IsNullOrEmpty(aiSummary))
                {
                    portfolio.AiProfileSummary = aiSummary;

                    await _portfolioRepo.UpdatePortfolioAsync(portfolio);

                    await _hubContext.Clients.All.SendAsync("PortfolioSummaryCompleted", studentId.ToString());
                    Console.WriteLine($"[SUCCESS] Đã tạo xong AI Master Summary cho Portfolio: {portfolio.PortfolioId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LỖI AI SUMMARY E-PORTFOLIO]: {ex.Message}");
            }
        }

        public async Task ProcessFullGithubPipelineAsync(Guid studentId, string githubUsername)
        {
            // Bước 1: Gọi hàm đồng bộ (Chỉ kéo danh sách repo và nội dung README thô về DB)
            var syncResult = await SyncGithubReposAsync(studentId, githubUsername);
            if (syncResult.StatusCode != 200) return;

            await Task.Delay(1000);

            // Fetch portfolio tại đây để luôn có portfolioId truyền cho bước 3
            var portfolio = await _portfolioRepo.GetPortfolioByStudentIdAsync(studentId);
            if (portfolio == null) return;

            // Bước 2: Bổ sung cập nhật Ngôn ngữ lập trình thực tế từ GitHub API
            try
            {
                var github = new GitHubClient(new ProductHeaderValue("TechCompassApp"));
                var githubToken = _config["GithubConfig:PersonalAccessToken"];
                if (!string.IsNullOrEmpty(githubToken)) github.Credentials = new Credentials(githubToken);

                if (portfolio.GithubRepositories != null)
                {
                    foreach (var dbRepo in portfolio.GithubRepositories)
                    {
                        string[] urlParts = dbRepo.GithubUrl.Replace("https://github.com/", "").Split('/');
                        if (urlParts.Length >= 2)
                        {
                            string owner = urlParts[0];
                            string repoName = urlParts[1];

                            var languages = await github.Repository.GetAllLanguages(owner, repoName);
                            if (languages.Any())
                            {
                                dbRepo.ExtractedTechStack = string.Join(", ", languages.Select(l => l.Name));
                                await _portfolioRepo.UpdateGithubRepoAsync(dbRepo);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING-LANGUAGES] Không kéo được danh sách ngôn ngữ Octokit: {ex.Message}");
            }

            // Bước 3: Tổng hợp và tính toán tỷ lệ % phù hợp nghề nghiệp
            Console.WriteLine("[MASTER-PIPELINE] Bắt đầu đánh giá mức độ phù hợp Job Role...");
            await EvaluateRoleSuitabilityAsync(studentId, portfolio.PortfolioId);
        }

        public async Task EvaluateRoleSuitabilityAsync(Guid studentId, Guid portfolioId)
        {
            var portfolio = await _portfolioRepo.GetPortfolioByStudentIdAsync(studentId);
            if (portfolio == null) return;

            // 1. Lấy năng lực học thuật ẩn (Latent Talent)
            string academicTalent = portfolio.Student?.LatentTalentSummary ?? "Chưa có dữ liệu học thuật.";

            // 2. Lấy dữ liệu các dự án thực tế đã được AI phân tích thành công
            var validRepos = portfolio.GithubRepositories?
                .Where(r => !string.IsNullOrEmpty(r.AiProjectSummary))
                .ToList() ?? new List<GithubRepository>();

            string projectContext = validRepos.Any()
                ? string.Join("\n", validRepos.Select(r => $"- Dự án '{r.RepoName}': {r.AiProjectSummary} (Công nghệ: {r.ExtractedTechStack})"))
                : "Chưa có dự án thực tế nào được phân tích.";

            // 3. Prompt thiết kế theo chuẩn Nghiên cứu Hướng nghiệp
            string prompt = $@"Bạn là một chuyên gia Định hướng nghề nghiệp IT cấp cao. Dựa trên hồ sơ của sinh viên, hãy thực hiện phân tích định lượng.

[HỒ SƠ HỌC THUẬT & TƯ DUY ẨN]:
{academicTalent}

[KINH NGHIỆM DỰ ÁN THỰC CHIẾN]:
{projectContext}

YÊU CẦU BẮT BUỘC: 
1. Đưa ra đánh giá tổng quan (Professional Summary) ngắn gọn trong 3-4 câu (KHÔNG dùng markdown).
2. Tính toán phần trăm mức độ phù hợp (0% - 100%) cho 3 vị trí dựa trên dữ liệu thực tế: Backend Developer, Front-End Developer, Full-Stack Developer.
3. Trả về đúng định dạng text thô, không giải thích dông dài, cấu trúc chính xác như sau:
SUMMARY: [Nội dung đoạn văn tóm tắt hồ sơ năng lực của sinh viên]
SUITABILITY:
Backend Developer: [Số]%
Front-End Developer: [Số]%
Full-Stack Developer: [Số]%";

            try
            {
                var chatService = _kernel.GetRequiredService<IChatCompletionService>("GeminiChat");
                var result = await chatService.GetChatMessageContentAsync(prompt);
                string aiResponse = result.Content?.Trim() ?? string.Empty;

                if (!string.IsNullOrEmpty(aiResponse) && aiResponse.Contains("SUMMARY:") && aiResponse.Contains("SUITABILITY:"))
                {
                    var parts = aiResponse.Split(new[] { "SUITABILITY:" }, StringSplitOptions.None);
                    string cleanSummary = parts[0].Replace("SUMMARY:", "").Trim();

                    portfolio.AiProfileSummary = cleanSummary;
                    await _portfolioRepo.UpdatePortfolioAsync(portfolio);

                    // Bắn SignalR thông báo hoàn tất toàn bộ tiến trình định hướng nghề nghiệp cao cấp
                    await _hubContext.Clients.All.SendAsync("portfoliosummarycompleted", studentId.ToString());
                    Console.WriteLine($"[RESEARCH SUCCESS] Đã phân tích xong độ khớp vai trò cho Student: {studentId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RESEARCH ERROR] Lỗi phân tích định hướng nghề nghiệp: {ex.Message}");
            }
        }
    }
}