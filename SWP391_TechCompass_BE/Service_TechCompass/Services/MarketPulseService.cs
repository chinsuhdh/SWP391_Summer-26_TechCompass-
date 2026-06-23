using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Repository_TechCompass;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Service_TechCompass.Services
{
    public class MarketPulseService : IMarketPulseService
    {
        private readonly IMarketPulseRepository _repo;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly Swp391CareerRoadmapContext _context;
        private readonly ITelemetryService _telemetryService;
        private readonly IChatCompletionService _geminiService; // ĐÃ THÊM: Inject Semantic Kernel

        public MarketPulseService(
            IMarketPulseRepository repo,
            HttpClient httpClient,
            IConfiguration config,
            Swp391CareerRoadmapContext context,
            ITelemetryService telemetryService,
            Kernel kernel) // ĐÃ THÊM: Kernel
        {
            _repo = repo;
            _httpClient = httpClient;
            _config = config;
            _context = context;
            _telemetryService = telemetryService;

            // Lấy service Gemini từ cấu hình tập trung trong Program.cs
            _geminiService = kernel.GetRequiredService<IChatCompletionService>("GeminiChat");
        }

        // ==========================================
        // 1. AI JOB MATCHING 
        // ==========================================
        public async Task<List<JobMatchDto>> GetMatchingJobsAsync(Guid studentId, JobFilterDto filter)
        {
            var student = await _repo.GetStudentWithPassedSkillsAsync(studentId);
            if (student == null) throw new Exception("Không tìm thấy sinh viên.");

            var mySkills = await _context.RoadmapProgresses
                .Include(p => p.SkillNode)
                .Where(p => p.StudentId == studentId && p.Status == "Completed")
                .Select(p => p.SkillNode.NodeName)
                .ToListAsync();

            int skip = (filter.Page - 1) * filter.PageSize;
            var jobs = await _repo.GetJobPostingsAsync(filter.Keyword, filter.SourcePlatform, skip, filter.PageSize);
            var result = new List<JobMatchDto>();

            foreach (var job in jobs)
            {
                var jobSkills = job.SkillNodes.Select(s => s.NodeName).ToList();

                if (filter.Skills != null && filter.Skills.Any())
                {
                    if (!jobSkills.Intersect(filter.Skills, StringComparer.OrdinalIgnoreCase).Any()) continue;
                }

                var matched = jobSkills.Intersect(mySkills, StringComparer.OrdinalIgnoreCase).ToList();
                var missing = jobSkills.Except(mySkills, StringComparer.OrdinalIgnoreCase).ToList();
                decimal matchPercent = jobSkills.Count > 0 ? (decimal)matched.Count / jobSkills.Count * 100 : 0;

                if (filter.MinMatch > 0 && matchPercent < filter.MinMatch) continue;

                result.Add(new JobMatchDto
                {
                    PostingId = job.PostingId,
                    JobTitle = job.JobTitle,
                    CompanyName = job.CompanyName ?? "Công ty bảo mật",
                    SourcePlatform = job.SourcePlatform,
                    MatchPercentage = Math.Round(matchPercent, 2),
                    MatchedSkills = matched,
                    MissingSkills = missing
                });
            }

            // Ghi log User vừa lọc Job 
            await _telemetryService.LogLearningHistoryAsync(
                studentId: studentId,
                progressId: Guid.Empty,
                actionType: "FILTER_JOB_MARKET",
                durationSeconds: 0,
                details: $"Sinh viên vừa tìm kiếm job với từ khóa '{filter.Keyword}'"
            );

            return filter.SortBy == "match"
                ? result.OrderByDescending(x => x.MatchPercentage).ToList()
                : result.OrderByDescending(x => x.PostingId).ToList();
        }

        // ==========================================
        // 2. DYNAMIC SCRAPING (Cào dữ liệu đa nền tảng)
        // ==========================================
        public async Task<(int StatusCode, string Message)> RunScraperAndTrendAnalysisAsync()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                string serpApiKey = _config["SerpApiConfig:ApiKey"]!;
                string serpBaseUrl = _config["SerpApiConfig:BaseUrl"] ?? "https://serpapi.com/search.json";

                var allRoles = await _context.TargetCareerRoles.Select(r => r.RoleName).ToListAsync();
                if (!allRoles.Any()) allRoles = new List<string> { "Software Engineer" };

                var dynamicKeywords = new List<string>();
                foreach (var role in allRoles)
                {
                    string shortRole = role.Split('(')[0].Split('-')[0].Trim();
                    dynamicKeywords.Add(shortRole);
                    dynamicKeywords.Add($"Tuyển dụng {shortRole}");
                    dynamicKeywords.Add($"Việc làm {shortRole}");
                }

                string query = dynamicKeywords[new Random().Next(dynamicKeywords.Count)];
                var locations = new[] { "Ho Chi Minh City, Vietnam", "Hanoi, Vietnam", "Da Nang, Vietnam", "Vietnam" };
                string location = locations[new Random().Next(locations.Length)];

                string requestUrl = $"{serpBaseUrl}?engine=google_jobs&q={Uri.EscapeDataString(query)}&location={Uri.EscapeDataString(location)}&gl=vn&api_key={serpApiKey}";

                var response = await _httpClient.GetAsync(requestUrl);
                if (!response.IsSuccessStatusCode) return (500, "Lỗi khi gọi API cào dữ liệu từ SerpApi.");

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var serpData = JsonSerializer.Deserialize<SerpApiResponseDto>(jsonResponse);

                if (serpData?.JobsResults == null || !serpData.JobsResults.Any())
                {
                    return (404, $"Không tìm thấy việc làm mới cho từ khóa '{query}' tại '{location}'.");
                }

                var allNodes = await _repo.GetAllSkillNodesAsync();
                var trendsToSave = new List<TrendAnalysis>();
                int newJobsCount = 0;

                foreach (var scrapedJob in serpData.JobsResults.Take(10))
                {
                    var job = new JobPosting
                    {
                        PostingId = Guid.NewGuid(),
                        JobTitle = scrapedJob.Title ?? "Vị trí lập trình viên",
                        CompanyName = scrapedJob.CompanyName ?? "Công ty công nghệ",
                        SourcePlatform = scrapedJob.Source ?? "Google Jobs",
                        JobDescriptionRaw = scrapedJob.Description ?? "",
                        ScrapedAt = DateTime.Now
                    };

                    var matchedNodes = await ExtractSkillsUsingAiAsync(job.JobDescriptionRaw, allNodes);

                    foreach (var node in matchedNodes)
                    {
                        job.SkillNodes.Add(node);
                        trendsToSave.Add(new TrendAnalysis
                        {
                            SkillNodeId = node.SkillNodeId,
                            AnalyzedDate = DateOnly.FromDateTime(DateTime.Now),
                            DemandPercent = (decimal)new Random().Next(10, 90),
                            TrendScore = (decimal)(new Random().NextDouble() * 5)
                        });
                    }

                    await _repo.SaveJobPostingAsync(job);
                    newJobsCount++;
                }

                if (trendsToSave.Any()) await _repo.SaveTrendAnalysisAsync(trendsToSave);

                watch.Stop();

                await _telemetryService.LogLearningHistoryAsync(
                    studentId: Guid.Empty,
                    progressId: Guid.Empty,
                    actionType: "SYSTEM_JOB_SCRAPED",
                    durationSeconds: (int)watch.Elapsed.TotalSeconds,
                    details: $"CronJob cào thành công {newJobsCount} công việc cho nhóm ngành '{query}' tại '{location}'."
                );

                return (200, $"Cào thành công {newJobsCount} công việc cho nhóm ngành '{query}' tại '{location}'.");
            }
            catch (Exception ex)
            {
                return (500, $"Lỗi hệ thống trong quá trình cào dữ liệu: {ex.Message}");
            }
        }

        // ==========================================
        // 3. TRÍCH XUẤT KỸ NĂNG BẰNG AI
        // ==========================================
        private async Task<List<SkillNode>> ExtractSkillsUsingAiAsync(string description, List<SkillNode> allNodes)
        {
            if (string.IsNullOrWhiteSpace(description)) return new List<SkillNode>();

            string availableSkills = string.Join(", ", allNodes.Select(n => n.NodeName));
            string prompt = $@"Bạn là một hệ thống tự động. Dưới đây là danh sách các kỹ năng hệ thống có: [{availableSkills}].
Nhiệm vụ: Đọc đoạn mô tả công việc sau và trích xuất TẤT CẢ các kỹ năng công nghệ có xuất hiện trong đoạn mô tả và trùng khớp (hoặc gần giống) với danh sách trên.
ĐỊNH DẠNG TRẢ VỀ: Chỉ in ra tên các kỹ năng, ngăn cách nhau bằng DẤU PHẨY. Tuyệt đối KHÔNG có câu chào hỏi, KHÔNG có bullet point, KHÔNG giải thích.
Ví dụ: C#, .NET Core, SQL Server
Mô tả công việc: {description}";

            try
            {
                // SỬ DỤNG SEMANTIC KERNEL THAY VÌ HTTPCLIENT THỦ CÔNG
                var chatHistory = new ChatHistory();
                chatHistory.AddUserMessage(prompt);

                var response = await _geminiService.GetChatMessageContentAsync(chatHistory);
                string aiText = response.ToString() ?? "";

                var extractedSkillNames = aiText.Split(',').Select(s => s.Trim().ToLower()).ToList();
                return allNodes.Where(n => extractedSkillNames.Contains(n.NodeName.ToLower())).ToList();
            }
            catch
            {
                /* Fallback */
            }

            return new List<SkillNode>();
        }

        public async Task<List<TrendChartDto>> GetTrendChartDataAsync(int days = 30)
        {
            var fromDate = DateTime.Now.AddDays(-days);
            var rawTrends = await _repo.GetTrendsForChartAsync(fromDate);
            var grouped = rawTrends.GroupBy(t => t.SkillNode.NodeName)
                .Select(g => new TrendChartDto
                {
                    NodeName = g.Key,
                    DataPoints = g.Select(x => new TrendPointDto
                    {
                        AnalyzedDate = x.AnalyzedDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue,
                        DemandPercent = x.DemandPercent ?? 0,
                        TrendScore = x.TrendScore ?? 0
                    }).OrderBy(x => x.AnalyzedDate).ToList()
                }).ToList();
            return grouped;
        }
    }
}