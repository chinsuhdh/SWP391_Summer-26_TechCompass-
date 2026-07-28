// Service_TechCompass/Services/PortfolioService.cs
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

        // =========================================================
        // [BUG-014 FIX]: ĐỌC FRONTEND BASE URL TỪ CONFIGURATION
        // =========================================================
        public async Task<string> GenerateShareableUrlAsync(Guid studentId)
        {
            var p = await _portfolioRepo.GetPortfolioByStudentIdAsync(studentId)
                    ?? await _portfolioRepo.CreatePortfolioAsync(new EPortfolio
                    {
                        PortfolioId = Guid.NewGuid(),
                        StudentId = studentId,
                        CreatedAt = DateTime.Now
                    });

            // 1. Đọc Base URL từ AppSettings (Kiểm tra nhiều key dự phòng)
            string baseUrl = _config["AppConfig:FrontendBaseUrl"]
                          ?? _config["FrontendBaseUrl"]
                          ?? "https://techcompass.com";

            // 2. Chuẩn hóa bỏ dấu '/' ở cuối nếu có
            baseUrl = baseUrl.TrimEnd('/');

            // 3. Sinh URL định danh duy nhất dựa trên Config
            p.ShareableUrl = $"{baseUrl}/p/{Guid.NewGuid().ToString("N")[..8]}";

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
            var portfolio = await _portfolioRepo.GetPortfolioByIdAsync(portfolioId);
            if (portfolio == null)
            {
                throw new ArgumentException("Portfolio không tồn tại.");
            }

            Guid studentId = portfolio.StudentId;

            var session = new MentorSession
            {
                SessionId = Guid.NewGuid(),
                MentorId = mentorUserId,
                StudentId = studentId,
                ScheduledAt = DateTime.UtcNow,
                DurationMinutes = 0,
                Status = "Completed",
                ReviewNotes = dto.Content,
                PaymentStatus = "Free"
            };

            bool isSaved = await _portfolioRepo.SaveFeedbackSessionAsync(session);

            if (isSaved)
            {
                await _hubContext.Clients.User(studentId.ToString()).SendAsync("ReceiveNewFeedback", new
                {
                    PortfolioId = portfolioId,
                    MentorId = mentorUserId,
                    Message = "Bạn vừa nhận được nhận xét mới từ Mentor!",
                    FeedbackContent = dto.Content,
                    Timestamp = DateTime.UtcNow
                });

                return new PortfolioFeedbackResponseDto
                {
                    FeedbackId = session.SessionId,
                    PortfolioId = portfolioId,
                    MentorId = mentorUserId,
                    MentorName = "Mentor",
                    Content = dto.Content,
                    CreatedAt = session.ScheduledAt ?? DateTime.UtcNow
                };
            }

            throw new Exception("Lỗi hệ thống khi lưu nhận xét vào cơ sở dữ liệu.");
        }
        #endregion

        #region 1. MASTER PIPELINE
        public async Task ProcessFullGithubPipelineAsync(Guid studentId, string githubUsername)
        {
            try
            {
                var syncResult = await SyncGithubReposAsync(studentId, githubUsername);
                if (syncResult.StatusCode != 200)
                {
                    await _hubContext.Clients.All.SendAsync("PipelineFailed", studentId, syncResult.Message);
                    return;
                }

                await _hubContext.Clients.All.SendAsync("PipelineCompleted", studentId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PIPELINE ERROR]: {ex.Message}");
                await _hubContext.Clients.All.SendAsync("PipelineFailed", studentId, "Lỗi trong quá trình đồng bộ. Vui lòng thử lại sau.");
            }
        }
        #endregion

        #region 2. GITHUB SYNC (RAW DATA FETCHING)
        public async Task<(int StatusCode, string Message)> SyncGithubReposAsync(Guid studentId, string githubUsername)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            // =====================================================================
            // [SECURITY-FIX #1]: XÁC THỰC CHỦ SỞ HỮU GITHUB USERNAME
            // =====================================================================
            var student = await _portfolioRepo.GetStudentByIdAsync(studentId);
            if (student == null)
                return (404, "Không tìm thấy hồ sơ sinh viên.");

            if (!string.IsNullOrWhiteSpace(student.GithubUsername))
            {
                // Sinh viên đã đăng ký Github username trước đó → bắt buộc phải khớp
                if (!student.GithubUsername.Equals(githubUsername.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return (403, $"GitHub username không khớp với hồ sơ đã đăng ký ('{student.GithubUsername}'). " +
                                  "Nếu muốn thay đổi, vui lòng cập nhật trong phần Thông tin cá nhân trước.");
                }
            }
            else
            {
                // =====================================================================
                // [SECURITY-FIX #2.5]: KIỂM TRA TÍNH DUY NHẤT TOÀN HỆ THỐNG
                // =====================================================================
                string cleanUsername = githubUsername.Trim();
                bool isTaken = await _portfolioRepo.IsGithubUsernameTakenAsync(cleanUsername);

                if (isTaken)
                {
                    return (409, $"Tài khoản GitHub '{cleanUsername}' đã được liên kết với một sinh viên khác trong hệ thống TechCompass. Không thể sử dụng chung!");
                }

                // Lần đầu sync và tên chưa ai dùng → tự động lưu username vào hồ sơ
                student.GithubUsername = cleanUsername;
                student.UpdatedAt = DateTime.Now;
                await _portfolioRepo.UpdateStudentAsync(student);
            }
            // =====================================================================

            var portfolio = await _portfolioRepo.GetPortfolioByStudentIdAsync(studentId)
                         ?? await _portfolioRepo.CreatePortfolioAsync(new EPortfolio { PortfolioId = Guid.NewGuid(), StudentId = studentId, CreatedAt = DateTime.Now });

            try
            {
                // Lấy danh sách Repo hiện có trong DB (đã include sẵn từ portfolio)
                var existingRepos = portfolio.GithubRepositories?.ToList() ?? new List<GithubRepository>();

                var github = new GitHubClient(new ProductHeaderValue("TechCompassApp"));
                var githubToken = _config["GithubConfig:PersonalAccessToken"];
                if (!string.IsNullOrEmpty(githubToken)) github.Credentials = new Credentials(githubToken);

                // Kéo danh sách từ GitHub về
                var incomingRepos = await github.Repository.GetAllForUser(githubUsername);
                var incomingRepoUrls = incomingRepos.Select(r => r.HtmlUrl).ToList();

                // =====================================================================
                // 1. [SMART-CLEANUP]: Xóa các repo rác (có trong DB nhưng không có trên GitHub đợt này)
                // =====================================================================
                var reposToDelete = existingRepos.Where(r => !incomingRepoUrls.Contains(r.GithubUrl)).ToList();
                foreach (var repoToDelete in reposToDelete)
                {
                    await _portfolioRepo.DeleteGithubRepoAsync(repoToDelete);
                }

                int syncCount = 0;

                // =====================================================================
                // 2. [UPSERT]: Thêm mới hoặc Cập nhật giữ nguyên AI Summary
                // =====================================================================
                foreach (var repo in incomingRepos)
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

                    var languages = await github.Repository.GetAllLanguages(repo.Owner.Login, repo.Name);
                    string actualTechStack = languages.Any() ? string.Join(", ", languages.Select(l => l.Name)) : string.Empty;

                    // Tìm xem repo này đã có trong DB chưa
                    var dbRepo = existingRepos.FirstOrDefault(r => r.GithubUrl == repo.HtmlUrl);

                    if (dbRepo != null)
                    {
                        // TRƯỜNG HỢP ĐÃ CÓ: Chỉ cập nhật data mới, KHÔNG CHẠM VÀO AiProjectSummary
                        dbRepo.RepoName = repo.Name;
                        dbRepo.ReadmeContent = readmeContent;
                        dbRepo.ExtractedTechStack = actualTechStack;
                        dbRepo.SyncedAt = DateTime.Now;

                        await _portfolioRepo.UpdateGithubRepoAsync(dbRepo);
                    }
                    else
                    {
                        // TRƯỜNG HỢP MỚI: Insert bình thường
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

        #region 3. AI REPOSITORY ANALYSIS & CẬP NHẬT HỒ SƠ TỔNG THỂ
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
                catch { }

                repo.AiProjectSummary = aiResponse;
                await _portfolioRepo.UpdateGithubRepoAsync(repo);

                // =========================================================================
                // [LOGIC MỚI]: CHUỖI COMBO - TÍNH TOÁN LẠI ĐIỂM SỐ & LỘ TRÌNH NGAY LẬP TỨC
                // =========================================================================
                var portfolio = await _portfolioRepo.GetPortfolioByIdAsync(repo.PortfolioId);
                if (portfolio != null)
                {
                    await EvaluateRoleSuitabilityAsync(portfolio.StudentId, portfolio.PortfolioId);
                    await GenerateEPortfolioSummaryAsync(portfolio.StudentId, portfolio.PortfolioId);

                    await _hubContext.Clients.All.SendAsync("AnalysisCompleted", portfolio.StudentId);
                }
                else
                {
                    await _hubContext.Clients.All.SendAsync("AnalysisCompleted", Guid.Empty);
                }
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

            string analysisContext = portfolio.AiProfileSummary;

            string prompt = $@"Dựa trên dữ liệu phân tích năng lực sau:
{analysisContext}
Hãy viết một đoạn 'Professional Summary' dài 150-200 từ, đóng vai trò là lời giới thiệu trên E-Portfolio.
Yêu cầu: Không dùng Markdown. Văn phong chuyên nghiệp, truyền cảm hứng, nêu bật định hướng nghề nghiệp.";

            string summaryText = await CallGeminiAsync(prompt);

            if (!string.IsNullOrEmpty(summaryText))
            {
                string safeSummaryText = summaryText.Replace("\"", "'").Replace("\n", " ").Replace("\r", "");

                var finalObject = new
                {
                    ProfileSummaryText = summaryText.Trim(),
                    AnalysisData = JsonSerializer.Deserialize<JsonElement>(portfolio.AiProfileSummary)
                };
                portfolio.AiProfileSummary = JsonSerializer.Serialize(finalObject);

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

            dto.GithubStats = new GithubStatsDto
            {
                TotalRepositories = repoList.Count,
                TotalLanguages = repoList.SelectMany(r => (r.ExtractedTechStack ?? "").Split(","))
                                         .Where(s => !string.IsNullOrWhiteSpace(s))
                                         .Select(s => s.Trim()).Distinct().Count(),
                TotalStars = 0,
                TotalCommits = 0,
                LastActive = repoList.Max(r => r.SyncedAt)?.ToString("dd/MM/yyyy")
            };

            dto.AcademicHighlights = new AcademicHighlightDto
            {
                Gpa = GetStudentGpa(entity),
                TopSubjects = new List<string>(),
                WeakSubjects = new List<string>()
            };

            string targetRole = dto.CareerRecommendation?.RecommendedRole ?? "Software Engineer";
            var currentSkills = repoList.SelectMany(r => (r.ExtractedTechStack ?? "").Split(",")).Select(s => s.Trim().ToUpper()).Distinct().ToList();
            var aiStrengths = dto.CareerRecommendation?.Strengths ?? new List<string>();
            var aiImprovements = dto.CareerRecommendation?.Improvements ?? new List<string>();

            dto.SkillGapAnalysis = new SkillGapReportDto
            {
                TargetRole = targetRole,
                MatchedSkills = currentSkills.Any() ? currentSkills : aiStrengths,
                MissingSkills = aiImprovements,
                MatchPercentage = CalculateMatchPercentage(currentSkills.Count, aiImprovements.Count)
            };

            dto.RoadmapProgress = new RoadmapProgressDto
            {
                RoadmapName = $"{targetRole} Roadmap",
                CompletedNodes = currentSkills.Count,
                InProgressNodes = 0,
                RemainingNodes = aiImprovements.Count,
                ProgressPercentage = dto.SkillGapAnalysis.MatchPercentage
            };

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

            int baseScore = 50;
            int skillScore = (dto.SkillGapAnalysis?.MatchPercentage ?? 0) * 30 / 100;
            int repoScore = Math.Min((dto.GithubStats?.TotalRepositories ?? 0) * 2, 10);
            int difficultyScore = repoList.Any() ? (int)Math.Round(repoList.Average(r => r.DifficultyStars)) * 2 : 0;

            dto.AiCareerScore = Math.Min(baseScore + skillScore + repoScore + difficultyScore, 99);

            return dto;
        }

        private string GetStudentDisplayName(dynamic entity)
        {
            try
            {
                var student = entity.Student;
                if (student != null)
                {
                    if (HasProperty(student, "FullName") && !string.IsNullOrEmpty(student.FullName)) return student.FullName;
                    if (HasProperty(student, "Name") && !string.IsNullOrEmpty(student.Name)) return student.Name;
                    if (HasProperty(student, "Email") && !string.IsNullOrEmpty(student.Email)) return student.Email.Split('@')[0];
                }
            }
            catch { }
            return "Sinh viên TechCompass";
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

        #region 7. AI FEEDBACK SUGGESTION (FOR MENTOR)
        public async Task<string> GenerateAiFeedbackSuggestionAsync(Guid portfolioId)
        {
            var portfolio = await _portfolioRepo.GetPortfolioByIdAsync(portfolioId);
            if (portfolio == null) return "Không tìm thấy Portfolio.";

            string profileData = portfolio.AiProfileSummary ?? "Chưa có summary.";
            string repos = portfolio.GithubRepositories != null && portfolio.GithubRepositories.Any()
                ? string.Join("\n", portfolio.GithubRepositories.Select(r => $"- {r.RepoName}: {r.ExtractedTechStack}"))
                : "Chưa có dự án.";

            string prompt = $@"Bạn là một Mentor IT Senior. Hãy viết MỘT đoạn nhận xét (feedback) ngắn gọn, chuyên nghiệp và mang tính xây dựng cho sinh viên dựa trên E-Portfolio sau:

[TỔNG QUAN AI ĐÁNH GIÁ]: {profileData}
[DỰ ÁN GITHUB]: {repos}

Yêu cầu:
- Xưng hô: 'Chào em,' hoặc 'Chào bạn,'
- Nêu bật 1-2 điểm mạnh trong code/tech stack.
- Chỉ ra 1 điểm cần cải thiện (thiếu testing, design pattern, hoặc cần học thêm tech gì).
- Trả về dạng text thuần (Plain text), KHÔNG dùng định dạng markdown ```json hay bôi đậm. Độ dài khoảng 100-150 từ.";

            string aiDraft = await CallGeminiAsync(prompt);
            return aiDraft;
        }
        #endregion
    }
}