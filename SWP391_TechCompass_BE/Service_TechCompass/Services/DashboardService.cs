using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Repository_TechCompass;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly Swp391CareerRoadmapContext _context;
        private readonly IMemoryCache _cache;
        private readonly IChatCompletionService _geminiService;

        public DashboardService(
            Swp391CareerRoadmapContext context,
            IMemoryCache cache,
            Kernel kernel)
        {
            _context = context;
            _cache = cache;
            _geminiService = kernel.GetRequiredService<IChatCompletionService>("GeminiChat");
        }

        public async Task<DashboardOverviewDto> GetOverviewAsync(Guid studentId)
        {
            var result = new DashboardOverviewDto();

            // 1. LẤY THÔNG TIN SINH VIÊN VÀ MỤC TIÊU
            var profile = await _context.Students
                .Include(p => p.TargetRole)
                .FirstOrDefaultAsync(p => p.StudentId == studentId);

            result.TargetRoleName = profile?.TargetRole?.RoleName ?? "Chưa xác định";

            // Lấy toàn bộ tiến độ lộ trình
            var allRoadmapNodes = await _context.RoadmapProgresses
                .Include(p => p.SkillNode)
                .Where(p => p.StudentId == studentId)
                .ToListAsync();

            int totalNodes = allRoadmapNodes.Count;
            int completedNodes = allRoadmapNodes.Count(p => p.Status == "Completed");

            // Tính Roadmap Score (Tỷ lệ hoàn thành lộ trình)
            result.RoadmapScore = totalNodes > 0 ? (int)Math.Round((double)completedNodes / totalNodes * 100) : 0;

            // 2. TÍNH TOÁN PORTFOLIO SCORE & HEALTH TỪ GITHUB REPOS
            var portfolio = await _context.EPortfolios
                .Include(p => p.GithubRepositories)
                .FirstOrDefaultAsync(p => p.StudentId == studentId);

            var repos = portfolio?.GithubRepositories ?? new List<GithubRepository>();

            result.PortfolioHealth = new PortfolioHealthOverviewDto();
            result.CareerFits = new List<CareerFitDto>();

            if (repos.Any())
            {
                int goodReadmes = repos.Count(r => !string.IsNullOrEmpty(r.ReadmeContent) && r.ReadmeContent.Length > 100);
                int aiAnalyzed = repos.Count(r => !string.IsNullOrEmpty(r.AiProjectSummary));
                int hasTesting = repos.Count(r => (r.ExtractedTechStack ?? "").Contains("test", StringComparison.OrdinalIgnoreCase) ||
                                                  (r.ReadmeContent ?? "").Contains("test", StringComparison.OrdinalIgnoreCase));

                result.PortfolioHealth.Architecture = aiAnalyzed > 0 ? (aiAnalyzed == repos.Count ? "Good" : "Average") : "Poor";
                result.PortfolioHealth.Readme = goodReadmes > 0 ? (goodReadmes == repos.Count ? "Good" : "Poor") : "Missing";
                result.PortfolioHealth.Testing = hasTesting > 0 ? "Good" : "Missing";

                // Chấm điểm Portfolio tự động
                result.PortfolioScore = Math.Min(100, (repos.Count * 10) + (goodReadmes * 10) + (hasTesting * 20));

                // Gợi ý cải thiện động
                if (result.PortfolioHealth.Readme != "Good")
                {
                    result.PortfolioHealth.AiSuggestion = "Một số dự án đang thiếu hoặc file README quá ngắn. Bổ sung để tăng độ chuyên nghiệp.";
                    result.PortfolioHealth.EstimatedTime = "20 phút";
                    result.PortfolioHealth.Impact = "+10 Portfolio Score";
                }
                else if (result.PortfolioHealth.Testing == "Missing")
                {
                    result.PortfolioHealth.AiSuggestion = "Các dự án của bạn chưa thấy dấu vết của Unit Test. Bổ sung xUnit/NUnit sẽ là điểm cộng lớn.";
                    result.PortfolioHealth.EstimatedTime = "2 giờ";
                    result.PortfolioHealth.Impact = "+20 Portfolio Score";
                }
                else
                {
                    result.PortfolioHealth.AiSuggestion = "Portfolio của bạn đang tối ưu rất tốt. Hãy duy trì!";
                    result.PortfolioHealth.EstimatedTime = "N/A";
                    result.PortfolioHealth.Impact = "Duy trì phong độ";
                }
            }
            else
            {
                result.PortfolioScore = 0;
                result.PortfolioHealth.AiSuggestion = "Chưa có dự án nào được đồng bộ. Hãy kết nối GitHub để AI phân tích.";
            }

            // 3. BÓC TÁCH CAREER FIT & MARKET MATCH TỪ JSON
            if (portfolio != null && !string.IsNullOrEmpty(portfolio.AiProfileSummary) && portfolio.AiProfileSummary.StartsWith("{"))
            {
                try
                {
                    using var doc = JsonDocument.Parse(portfolio.AiProfileSummary);
                    if (doc.RootElement.TryGetProperty("AnalysisData", out var analysisData) &&
                        analysisData.TryGetProperty("Suitabilities", out var suit))
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        result.CareerFits = JsonSerializer.Deserialize<List<CareerFitDto>>(suit.GetRawText(), options) ?? new();

                        var targetFit = result.CareerFits.FirstOrDefault(f => f.RoleName.Contains(result.TargetRoleName, StringComparison.OrdinalIgnoreCase));
                        result.MarketMatchScore = targetFit?.MatchPercentage ?? 0;
                    }
                }
                catch { /* Fallback nếu JSON lỗi */ }
            }

            // 4. NEXT ACTION VÀ EXPLAINABLE AI BẰNG GEMINI
            var nextNode = allRoadmapNodes
                .Where(p => p.Status != "Completed")
                .OrderBy(p => p.SkillNode.PriorityLevel)
                .Select(p => p.SkillNode)
                .FirstOrDefault();

            if (nextNode != null)
            {
                result.NextAction = new NextActionDto
                {
                    NodeId = nextNode.SkillNodeId,
                    NodeName = nextNode.NodeName,
                    Difficulty = "Intermediate",
                    EstimatedHours = 4,
                    ExpectedReward = "+5 Readiness, +2 Portfolio"
                };

                // Lấy AI Reasoning từ Cache để tránh gọi Gemini liên tục làm chậm web
                string reasoningKey = $"AiReasoning_{nextNode.SkillNodeId}_{studentId}";
                if (!_cache.TryGetValue(reasoningKey, out List<string> reasoningBullets))
                {
                    string aiPrompt = $"Sinh viên IT đang hướng tới vị trí {result.TargetRoleName}. Kỹ năng tiếp theo cần học là {nextNode.NodeName}. Hãy viết đúng 3 dòng (mỗi dòng dưới 15 chữ, ngăn cách bằng dấu xuống dòng, không dùng gạch đầu dòng hay số) giải thích tại sao nhà tuyển dụng cần kỹ năng này.";
                    reasoningBullets = await GenerateBulletPointsFromGeminiAsync(aiPrompt, 3);

                    if (reasoningBullets.Any())
                        _cache.Set(reasoningKey, reasoningBullets, TimeSpan.FromDays(7));
                    else
                        reasoningBullets = new List<string> { $"{nextNode.NodeName} là kỹ năng cốt lõi.", "Giúp hoàn thiện kiến trúc hệ thống.", "Tăng tỷ lệ pass vòng CV." };
                }
                result.NextAction.AiReasoningBullets = reasoningBullets;
            }

            // 5. MARKET PULSE SUMMARY BẰNG GEMINI
            string pulseKey = $"MarketPulse_{result.TargetRoleName}";
            if (!_cache.TryGetValue(pulseKey, out string aiPulse))
            {
                string pulsePrompt = $"Viết 1 câu duy nhất (dưới 25 chữ) mô tả ngắn gọn xu hướng tuyển dụng hiện tại cho vị trí {result.TargetRoleName} tại Việt Nam. Không chào hỏi.";
                aiPulse = await CallGeminiAsync(pulsePrompt);

                if (string.IsNullOrEmpty(aiPulse)) aiPulse = $"Thị trường đang khát nhân sự {result.TargetRoleName} có khả năng tối ưu hệ thống.";
                _cache.Set(pulseKey, aiPulse, TimeSpan.FromDays(1));
            }

            result.MarketPulse = new MarketPulseSummaryDto { AiPulseSummary = aiPulse };

            // 6. ĐỌC DỮ LIỆU ĐÃ CACHE TỪ HANGFIRE CHO READINESS VÀ HEADER SUMMARY
            string cacheKey = $"DashboardMetrics_{studentId}";
            if (_cache.TryGetValue(cacheKey, out DashboardOverviewDto cachedMetrics))
            {
                result.ReadinessScore = cachedMetrics.ReadinessScore;
                result.AiQuickSummary = cachedMetrics.AiQuickSummary;
            }
            else
            {
                // Fallback tính nhanh nếu Hangfire chưa chạy
                result.ReadinessScore = result.RoadmapScore;
                result.AiQuickSummary = "Tiếp tục duy trì tiến độ học tập để đạt chuẩn thị trường nhé!";
            }

            return result;
        }

        public async Task CalculateAndCacheDashboardMetricsAsync(Guid studentId)
        {
            var allRoadmapNodes = await _context.RoadmapProgresses
                .Include(p => p.SkillNode)
                .Where(p => p.StudentId == studentId)
                .ToListAsync();

            if (!allRoadmapNodes.Any()) return;

            int completedCount = allRoadmapNodes.Count(p => p.Status == "Completed");
            int readinessScore = (int)Math.Round((double)completedCount / allRoadmapNodes.Count * 100);

            var completedSkills = allRoadmapNodes.Where(p => p.Status == "Completed").Select(p => p.SkillNode.NodeName).ToList();
            var missingSkills = allRoadmapNodes.Where(p => p.Status != "Completed").Take(3).Select(p => p.SkillNode.NodeName).ToList();

            string skillsStr = string.Join(", ", completedSkills);
            string missingStr = string.Join(", ", missingSkills);

            string prompt = $@"
Bạn là AI Mentor. Một sinh viên đang có điểm Readiness Score là {readinessScore}%.
Điểm mạnh (đã học): {skillsStr}. 
Đang thiếu: {missingStr}.
Hãy viết MỘT câu duy nhất (dưới 30 chữ) nhận xét và động viên sinh viên này, tập trung vào hành động tiếp theo.
KHÔNG dùng bullet points, KHÔNG chào hỏi.";

            string aiSummary = await CallGeminiAsync(prompt);
            if (string.IsNullOrEmpty(aiSummary)) aiSummary = "Tiếp tục duy trì tiến độ học tập để đạt chuẩn thị trường nhé!";

            var metricsToCache = new DashboardOverviewDto
            {
                ReadinessScore = readinessScore,
                AiQuickSummary = aiSummary
            };

            string cacheKey = $"DashboardMetrics_{studentId}";
            _cache.Set(cacheKey, metricsToCache, TimeSpan.FromHours(24));
        }

        // ==========================================
        // PRIVATE UTILITIES
        // ==========================================
        private async Task<string> CallGeminiAsync(string prompt)
        {
            try
            {
                var chatHistory = new ChatHistory();
                chatHistory.AddUserMessage(prompt);
                var response = await _geminiService.GetChatMessageContentAsync(chatHistory);
                return response.ToString().Replace("\"", "").Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        private async Task<List<string>> GenerateBulletPointsFromGeminiAsync(string prompt, int expectedCount)
        {
            string rawText = await CallGeminiAsync(prompt);
            if (string.IsNullOrEmpty(rawText)) return new List<string>();

            // Tách câu dựa trên dấu xuống dòng, loại bỏ dòng rỗng
            var bullets = rawText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(s => s.Trim().TrimStart('-', '*', '1', '2', '3', '.', ' '))
                                 .Where(s => s.Length > 5)
                                 .Take(expectedCount)
                                 .ToList();
            return bullets;
        }
    }
}