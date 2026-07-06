// src/Service_TechCompass/Services/PortfolioService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
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

        #region PUBLIC INTERFACE METHODS (CRUD & CORE)
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

        public async Task<string> GenerateShareableUrlAsync(Guid studentId)
        {
            var p = await _portfolioRepo.GetPortfolioByStudentIdAsync(studentId)
                    ?? await _portfolioRepo.CreatePortfolioAsync(new EPortfolio { PortfolioId = Guid.NewGuid(), StudentId = studentId, CreatedAt = DateTime.Now });

            p.ShareableUrl = $"https://techcompass.com/p/{Guid.NewGuid().ToString("N")[..8]}";
            await _portfolioRepo.UpdatePortfolioAsync(p);
            return p.ShareableUrl;
        }

        public async Task<List<object>> GetAllPublicPortfoliosAsync()
        {
            // TODO: Phân hệ cho Mentor
            return await Task.FromResult(new List<object>());
        }

        public async Task<PortfolioFeedbackResponseDto> AddPortfolioFeedbackAsync(Guid portfolioId, Guid mentorUserId, CreatePortfolioFeedbackDto dto)
        {
            // TODO: Phân hệ cho Mentor
            return await Task.FromResult(new PortfolioFeedbackResponseDto());
        }
        #endregion

        #region 1. MASTER PIPELINE
        public async Task ProcessFullGithubPipelineAsync(Guid studentId, string githubUsername)
        {
            // Bước 1: Kéo Code từ GitHub (Raw Data + README)
            var syncResult = await SyncGithubReposAsync(studentId, githubUsername);
            if (syncResult.StatusCode != 200) return;

            var portfolio = await _portfolioRepo.GetPortfolioByStudentIdAsync(studentId);
            if (portfolio == null || portfolio.GithubRepositories == null) return;

            // Bước 2: AI Phân tích từng Repository chưa được phân tích (Bóc tách JSON: Domain, Complexity, Featured...)
            var unanalyzedRepos = portfolio.GithubRepositories.Where(r => string.IsNullOrEmpty(r.AiProjectSummary)).ToList();
            foreach (var repo in unanalyzedRepos)
            {
                await AnalyzeRepoWithAiAsync(repo.RepoId);
            }

            // Reload dữ liệu để lấy context mới nhất
            portfolio = await _portfolioRepo.GetPortfolioByStudentIdAsync(studentId);

            // Bước 3: Đánh giá độ phù hợp nghề nghiệp (Suitability & Latent Talent)
            await EvaluateRoleSuitabilityAsync(studentId, portfolio!.PortfolioId);

            // Bước 4: Viết Master Profile Summary và đóng gói toàn bộ Data
            await GenerateEPortfolioSummaryAsync(studentId, portfolio.PortfolioId);

            // Hoàn tất Pipeline, bắn SignalR cho UI hiển thị
            await _hubContext.Clients.User(studentId.ToString()).SendAsync("PipelineCompleted");
        }
        #endregion

        #region 2. GITHUB SYNC (RAW DATA FETCHING)
        public async Task<(int StatusCode, string Message)> SyncGithubReposAsync(Guid studentId, string githubUsername)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var portfolio = await _portfolioRepo.GetPortfolioByStudentIdAsync(studentId)
                         ?? await _portfolioRepo.CreatePortfolioAsync(new EPortfolio { PortfolioId = Guid.NewGuid(), StudentId = studentId, CreatedAt = DateTime.Now });

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
                    catch (NotFoundException) { continue; } // Bỏ qua nếu không có README

                    if (string.IsNullOrWhiteSpace(readmeContent) || readmeContent.Length < 50) continue;

                    // Fetch Ngôn ngữ lập trình chính xác từ Octokit (thay cho AI đoán)
                    var languages = await github.Repository.GetAllLanguages(repo.Owner.Login, repo.Name);
                    string actualTechStack = languages.Any() ? string.Join(", ", languages.Select(l => l.Name)) : string.Empty;

                    var dbRepo = existingRepos.FirstOrDefault(r => r.GithubUrl == repo.HtmlUrl);
                    if (dbRepo != null)
                    {
                        dbRepo.ReadmeContent = readmeContent;
                        dbRepo.ExtractedTechStack = actualTechStack; // Lưu TechStack thực tế
                        dbRepo.SyncedAt = DateTime.Now;
                        // Xóa Summary cũ để bắt AI đọc lại nếu README thay đổi
                        dbRepo.AiProjectSummary = null;
                        await _portfolioRepo.UpdateGithubRepoAsync(dbRepo);
                    }
                    else
                    {
                        await _portfolioRepo.SaveGithubRepoAsync(new GithubRepository
                        {
                            RepoId = Guid.NewGuid(),
                            PortfolioId = portfolio.PortfolioId,
                            RepoName = repo.Name,
                            GithubUrl = repo.HtmlUrl,
                            ExtractedTechStack = actualTechStack,
                            ReadmeContent = readmeContent,
                            SyncedAt = DateTime.Now
                        });
                    }
                    syncCount++;
                }
                watch.Stop();
                await _telemetryService.LogLearningHistoryAsync(studentId, null, "SYNC_GITHUB_SUCCESS", (int)watch.Elapsed.TotalSeconds, $"Đồng bộ {syncCount} repos");
                return (200, $"Đồng bộ thành công {syncCount} dự án.");
            }
            catch (Exception ex)
            {
                return (500, $"Lỗi kết nối GitHub: {ex.Message}");
            }
        }
        #endregion

        #region 3. AI REPOSITORY ANALYSIS (Generate JSON)
        public async Task<(int StatusCode, string Message)> AnalyzeRepoWithAiAsync(Guid repoId)
        {
            var repo = await _portfolioRepo.GetGithubRepoByIdAsync(repoId);
            if (repo == null || string.IsNullOrWhiteSpace(repo.ReadmeContent)) return (400, "Lỗi dữ liệu.");

            string prompt = $@"Đọc file README của dự án sau:
{repo.ReadmeContent}
Hãy phân tích và trả về DUY NHẤT một chuỗi JSON hợp lệ với cấu trúc sau (KHÔNG dùng markdown ```json):
{{
  ""Summary"": ""Tóm tắt dự án trong 2-3 câu ngắn gọn"",
  ""TechStack"": ""Liệt kê các công nghệ, framework, tools (cách nhau bằng dấu phẩy)"",
  ""DifficultyStars"": [Đánh giá độ phức tạp từ 1 đến 5],
  ""IsFeatured"": [true nếu dự án phức tạp/có giá trị thực tiễn, ngược lại false],
  ""Domain"": ""Lĩnh vực (ví dụ: E-Commerce, Education, AI)"",
  ""Contribution"": ""Các logic/chức năng chính đã được implement""
}}";

            var aiResponse = await CallGeminiAsync(prompt);
            if (!string.IsNullOrEmpty(aiResponse))
            {
                // Parse thử JSON để trích xuất TechStack lưu vào cột vật lý trong DB
                try
                {
                    using var doc = JsonDocument.Parse(aiResponse);
                    if (doc.RootElement.TryGetProperty("TechStack", out var techStackElement))
                    {
                        string ts = techStackElement.GetString() ?? "";
                        if (!string.IsNullOrWhiteSpace(ts))
                        {
                            repo.ExtractedTechStack = ts;
                        }
                    }
                }
                catch { /* Bỏ qua nếu lỗi parse, vẫn lưu nguyên raw JSON xuống AiProjectSummary */ }

                // Lưu chuỗi JSON phân tích vào cột AiProjectSummary
                repo.AiProjectSummary = aiResponse;
                await _portfolioRepo.UpdateGithubRepoAsync(repo);

                // FIX LỖI TREO UI: Bắn SignalR để tắt trạng thái "AI đang đọc..." trên Frontend
                await _hubContext.Clients.All.SendAsync("AnalysisCompleted");
            }

            return (200, "Phân tích AI hoàn tất.");
        }
        #endregion

        #region 4. CAREER SUITABILITY & LATENT TALENT (Generate JSON)
        public async Task EvaluateRoleSuitabilityAsync(Guid studentId, Guid portfolioId)
        {
            var portfolio = await _portfolioRepo.GetPortfolioByStudentIdAsync(studentId);
            if (portfolio == null) return;

            string academicTalent = portfolio.Student?.LatentTalentSummary ?? "Sinh viên có nền tảng cơ bản, đang trong quá trình phát triển năng lực cốt lõi.";
            var validRepos = portfolio.GithubRepositories?.Where(r => !string.IsNullOrEmpty(r.AiProjectSummary)).ToList() ?? new();

            string projectContext = validRepos.Any()
                ? string.Join("\n", validRepos.Select(r => $"- {r.RepoName} (Stack: {r.ExtractedTechStack}): {r.AiProjectSummary}"))
                : "Chưa có dự án.";

            string prompt = $@"Bạn là chuyên gia Định hướng Nghề nghiệp IT. Đánh giá hồ sơ của sinh viên:
[HỌC THUẬT]: {academicTalent}
[DỰ ÁN THỰC TẾ]: {projectContext}

Trả về DUY NHẤT một chuỗi JSON hợp lệ (KHÔNG có markdown ```json). Cấu trúc chính xác:
{{
  ""Suitabilities"": [
    {{ ""RoleName"": ""Backend Developer"", ""MatchPercentage"": [Số 0-100] }},
    {{ ""RoleName"": ""Front-End Developer"", ""MatchPercentage"": [Số 0-100] }},
    {{ ""RoleName"": ""Full-Stack Developer"", ""MatchPercentage"": [Số 0-100] }}
  ],
  ""Recommendation"": {{
    ""RecommendedRole"": ""[Chọn 1 role tốt nhất]"",
    ""ConfidencePercentage"": [Số 0-100],
    ""Reason"": ""[Giải thích chi tiết 2 câu]"",
    ""Strengths"": [""[Điểm mạnh 1]"", ""[Điểm mạnh 2]""],
    ""Improvements"": [""[Điểm yếu 1]"", ""[Điểm yếu 2]""]
  }},
  ""LatentTalent"": {{
    ""LogicalThinking"": [Số 0-100],
    ""SystemDesign"": [Số 0-100],
    ""ProblemSolving"": [Số 0-100],
    ""UiUxSense"": [Số 0-100],
    ""Communication"": [Số 0-100]
  }}
}}";

            var aiResponse = await CallGeminiAsync(prompt);
            if (!string.IsNullOrEmpty(aiResponse))
            {
                // Tạm thời lưu Analysis JSON vào AiProfileSummary
                portfolio.AiProfileSummary = aiResponse;
                await _portfolioRepo.UpdatePortfolioAsync(portfolio);
            }
        }
        #endregion

        #region 5. E-PORTFOLIO MASTER SUMMARY (Merge JSON)
        public async Task GenerateEPortfolioSummaryAsync(Guid studentId, Guid portfolioId)
        {
            var portfolio = await _portfolioRepo.GetPortfolioByStudentIdAsync(studentId);
            if (portfolio == null || string.IsNullOrEmpty(portfolio.AiProfileSummary)) return;

            // Truyền bản phân tích JSON (Suitability) hiện có vào để AI viết lời mở đầu
            string analysisContext = portfolio.AiProfileSummary;

            string prompt = $@"Dựa trên dữ liệu phân tích năng lực sau:
{analysisContext}
Hãy viết một đoạn 'Professional Summary' dài 150-200 từ, đóng vai trò là lời giới thiệu trên E-Portfolio.
Yêu cầu: Không dùng Markdown. Văn phong chuyên nghiệp, truyền cảm hứng, nêu bật định hướng nghề nghiệp.";

            string summaryText = await CallGeminiAsync(prompt);

            if (!string.IsNullOrEmpty(summaryText))
            {
                // Bọc lại thành 1 JSON tổng có chứa cả [Đoạn văn mở đầu] và [Dữ liệu phân tích]
                string safeSummaryText = summaryText.Replace("\"", "'").Replace("\n", " ").Replace("\r", "");

                string finalJsonData = $@"{{
                  ""ProfileSummaryText"": ""{safeSummaryText}"",
                  ""AnalysisData"": {portfolio.AiProfileSummary}
                }}";

                portfolio.AiProfileSummary = finalJsonData;
                await _portfolioRepo.UpdatePortfolioAsync(portfolio);
            }
        }
        #endregion

        #region 6. DATA AGGREGATION (MAP TO DTO)
        private PortfolioDto MapToDto(EPortfolio entity)
        {
            var dto = new PortfolioDto
            {
                PortfolioId = entity.PortfolioId,
                StudentId = entity.StudentId,
                // Ưu tiên lấy FullName từ bảng Student. Fallback dùng phần đầu của Email hoặc Email tĩnh nếu chưa có.
                StudentName = GetStudentDisplayName(entity),
                ShareableUrl = entity.ShareableUrl,
                CreatedAt = entity.CreatedAt,
                Repositories = new List<GithubRepoDto>()
            };

            var repoList = new List<GithubRepoDto>();
            if (entity.GithubRepositories != null)
            {
                foreach (var repo in entity.GithubRepositories)
                {
                    var repoDto = new GithubRepoDto
                    {
                        RepoId = repo.RepoId,
                        RepoName = repo.RepoName,
                        GithubUrl = repo.GithubUrl,
                        ExtractedTechStack = repo.ExtractedTechStack ?? string.Empty,
                        SyncedAt = repo.SyncedAt,
                        DifficultyStars = 3
                    };

                    if (!string.IsNullOrEmpty(repo.AiProjectSummary) && repo.AiProjectSummary.StartsWith("{"))
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(repo.AiProjectSummary);
                            var root = doc.RootElement;
                            repoDto.AiProjectSummary = root.GetProperty("Summary").GetString() ?? "";
                            if (root.TryGetProperty("DifficultyStars", out var diff)) repoDto.DifficultyStars = diff.GetInt32();
                            if (root.TryGetProperty("IsFeatured", out var feat)) repoDto.IsFeatured = feat.GetBoolean();
                        }
                        catch { repoDto.AiProjectSummary = repo.AiProjectSummary; }
                    }
                    else
                    {
                        repoDto.AiProjectSummary = repo.AiProjectSummary ?? "";
                    }
                    repoList.Add(repoDto);
                }
            }

            dto.Repositories = repoList.OrderByDescending(r => r.IsFeatured).ThenByDescending(r => r.DifficultyStars).ToList();

            if (!string.IsNullOrEmpty(entity.AiProfileSummary) && entity.AiProfileSummary.StartsWith("{"))
            {
                try
                {
                    using var doc = JsonDocument.Parse(entity.AiProfileSummary);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("ProfileSummaryText", out var sumText))
                        dto.AiProfileSummary = sumText.GetString();

                    if (root.TryGetProperty("AnalysisData", out var analysisData))
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        if (analysisData.TryGetProperty("Suitabilities", out var suit))
                            dto.CareerSuitabilities = JsonSerializer.Deserialize<List<CareerSuitabilityDto>>(suit.GetRawText(), options) ?? new();
                        if (analysisData.TryGetProperty("Recommendation", out var rec))
                            dto.CareerRecommendation = JsonSerializer.Deserialize<CareerRecommendationDto>(rec.GetRawText(), options);
                        if (analysisData.TryGetProperty("LatentTalent", out var talent))
                            dto.LatentTalent = JsonSerializer.Deserialize<LatentTalentDto>(talent.GetRawText(), options);
                    }
                }
                catch { dto.AiProfileSummary = "Hồ sơ đang trong quá trình AI phân tích..."; }
            }
            else
            {
                dto.AiProfileSummary = entity.AiProfileSummary;
            }

            // Dữ liệu GitHub tính toán thực tế từ danh sách Repo (Không random)
            dto.GithubStats = new GithubStatsDto
            {
                TotalRepositories = repoList.Count,
                TotalLanguages = repoList.SelectMany(r => (r.ExtractedTechStack ?? "").Split(","))
                                         .Where(s => !string.IsNullOrWhiteSpace(s))
                                         .Select(s => s.Trim()).Distinct().Count(),
                TotalStars = 0, // Giá trị thực nên lấy từ Octokit lúc Sync
                TotalCommits = 0, // Giá trị thực nên lấy từ Octokit lúc Sync
                LastActive = repoList.Max(r => r.SyncedAt)?.ToString("dd/MM/yyyy")
            };

            // Academic lấy trực tiếp từ DB Student (dùng Dynamic để parse các trường nếu Entity có)
            dto.AcademicHighlights = new AcademicHighlightDto
            {
                // Thay vì Hardcode 8.4, ta lấy Gpa thực, nếu không có để 0
                Gpa = GetStudentGpa(entity),
                TopSubjects = new List<string>(), // Đọc từ Transcripts DB (nếu có bảng)
                WeakSubjects = new List<string>()
            };

            // KẾT HỢP DỮ LIỆU AI ĐỂ LÀM SKILL GAP DYNAMIC
            string targetRole = dto.CareerRecommendation?.RecommendedRole ?? "Software Engineer";
            var currentSkills = repoList.SelectMany(r => (r.ExtractedTechStack ?? "").Split(",")).Select(s => s.Trim().ToUpper()).Distinct().ToList();
            var aiStrengths = dto.CareerRecommendation?.Strengths ?? new List<string>();
            var aiImprovements = dto.CareerRecommendation?.Improvements ?? new List<string>();

            dto.SkillGapAnalysis = new SkillGapReportDto
            {
                TargetRole = targetRole,
                // Dùng AI Strengths và TechStack làm Matched Skills
                MatchedSkills = currentSkills.Any() ? currentSkills : aiStrengths,
                // Dùng AI Improvements làm Missing Skills để recruiter thấy điểm yếu khách quan
                MissingSkills = aiImprovements,
                MatchPercentage = CalculateMatchPercentage(currentSkills.Count, aiImprovements.Count)
            };

            dto.RoadmapProgress = new RoadmapProgressDto
            {
                RoadmapName = $"{targetRole} Roadmap",
                CompletedNodes = currentSkills.Count, // Số lượng kỹ năng đã nắm bắt
                InProgressNodes = 0, // Lấy từ DB UserRoadmap
                RemainingNodes = aiImprovements.Count,
                ProgressPercentage = dto.SkillGapAnalysis.MatchPercentage
            };

            // Timeline Sinh động dựa trên thời gian thực tế của Data
            if (entity.CreatedAt.HasValue)
            {
                dto.CareerJourney.Add(new TimelineEventDto { Year = entity.CreatedAt.Value.Year.ToString(), EventTitle = "Khởi tạo định hướng", Description = "Bắt đầu xây dựng lộ trình sự nghiệp." });
            }
            if (repoList.Any())
            {
                dto.CareerJourney.Add(new TimelineEventDto { Year = repoList.Min(r => r.SyncedAt)?.Year.ToString() ?? DateTime.Now.Year.ToString(), EventTitle = "Thực chiến dự án", Description = $"Bắt đầu đẩy mạnh kỹ năng với {repoList.Count} dự án trên GitHub." });
            }
            if (dto.CareerRecommendation != null)
            {
                dto.CareerJourney.Add(new TimelineEventDto { Year = "Hiện tại", EventTitle = "Mục tiêu chuyên sâu", Description = $"AI xác định phù hợp nhất với vị trí {dto.CareerRecommendation.RecommendedRole}." });
            }

            // Viết ngay trước dòng: return dto;
            int baseScore = 50; // Điểm sàn
            int skillScore = (dto.SkillGapAnalysis?.MatchPercentage ?? 0) * 30 / 100; // Chiếm 30% trọng số
            int repoScore = Math.Min((dto.GithubStats?.TotalRepositories ?? 0) * 2, 10); // Tối đa 10 điểm
            int difficultyScore = repoList.Any() ? (int)Math.Round(repoList.Average(r => r.DifficultyStars)) * 2 : 0; // Tối đa 10 điểm

            dto.AiCareerScore = Math.Min(baseScore + skillScore + repoScore + difficultyScore, 99);

            return dto;
        }

        // --- Helper Methods Để Tránh Lỗi Compile Khi Truy Cập Entity.Student ---
        private string GetStudentDisplayName(dynamic entity)
        {
            try
            {
                // Cố gắng đọc FullName, nếu không có đọc Name, nếu không có đọc Email
                var student = entity.Student;
                if (student != null)
                {
                    if (HasProperty(student, "FullName") && !string.IsNullOrEmpty(student.FullName)) return student.FullName;
                    if (HasProperty(student, "Name") && !string.IsNullOrEmpty(student.Name)) return student.Name;
                    if (HasProperty(student, "Email") && !string.IsNullOrEmpty(student.Email)) return student.Email.Split('@')[0];
                }
            }
            catch { }
            return "Sinh viên TechCompass"; // Fallback cuối cùng
        }

        private decimal GetStudentGpa(dynamic entity)
        {
            try
            {
                if (entity.Student != null && HasProperty(entity.Student, "Gpa"))
                {
                    return (decimal)entity.Student.Gpa;
                }
            }
            catch { }
            return 0m;
        }

        private int CalculateMatchPercentage(int have, int missing)
        {
            if (have == 0 && missing == 0) return 0;
            int total = have + missing;
            return (have * 100) / total;
        }

        private bool HasProperty(object obj, string propertyName)
        {
            return obj.GetType().GetProperty(propertyName) != null;
        }
        #endregion

        #region UTILITIES
        private async Task<string> CallGeminiAsync(string prompt)
        {
            try
            {
                var chatService = _kernel.GetRequiredService<IChatCompletionService>("GeminiChat");
                var result = await chatService.GetChatMessageContentAsync(prompt);
                string text = result.Content?.Trim() ?? string.Empty;

                // Xóa bỏ wrapper markdown của Gemini nếu có để chuẩn hóa JSON
                if (text.StartsWith("```json", StringComparison.OrdinalIgnoreCase)) text = text.Substring(7);
                else if (text.StartsWith("```", StringComparison.OrdinalIgnoreCase)) text = text.Substring(3);
                if (text.EndsWith("```")) text = text.Substring(0, text.Length - 3);

                return text.Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GEMINI ERROR]: {ex.Message}");
                return string.Empty;
            }
        }
        #endregion


    }
}