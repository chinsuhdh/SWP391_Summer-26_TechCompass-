// Service_TechCompass/Services/AdminMonitorService.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Repository_TechCompass;
using Repository_TechCompass.Interfaces;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class AdminMonitorService : IAdminMonitorService
    {
        private readonly IUserRepository _userRepo;
        private readonly Swp391CareerRoadmapContext _context;
        private readonly IConfiguration _config;
        private readonly IChatCompletionService _geminiService;
        private readonly IMemoryCache _cache;

        public AdminMonitorService(
            IUserRepository userRepo,
            Swp391CareerRoadmapContext context,
            IConfiguration config,
            Kernel kernel,
            IMemoryCache cache)
        {
            _userRepo = userRepo;
            _context = context;
            _config = config;
            _cache = cache;
            _geminiService = kernel.GetRequiredService<IChatCompletionService>("GeminiChat");
        }

        // 1. Hàm cũ: Lấy danh sách AI Recommendations
        public Task<(int StatusCode, string Message, List<AiRecommendationDto>? Data)> GetAllAiRecommendationsAsync()
        {
            var data = _userRepo.GetAllAiRecommendations().Select(x => new AiRecommendationDto
            {
                RecommendationId = x.RecommendationId,
                StudentId = x.StudentId,
                RecommendationType = x.RecommendationType,
                ContentJson = x.ContentJson,
                GeneratedAt = x.GeneratedAt
            }).ToList();

            return Task.FromResult<(int, string, List<AiRecommendationDto>?)>((200, "Lấy dữ liệu AI Recommendations thành công.", data));
        }

        // 2. Hàm cũ: Lấy System Logs
        public async Task<(int StatusCode, string Message, List<SystemLogDto>? Data)> GetSystemLogsAsync()
        {
            var dbLogs = await _context.LearningHistories
                .OrderByDescending(x => x.RecordedAt)
                .Take(20)
                .Select(x => new SystemLogDto
                {
                    LogId = x.HistoryId,
                    LogLevel = x.ActionType.Contains("ERROR") || x.ActionType.Contains("FAIL") ? "ERROR"
                             : x.ActionType.Contains("SYSTEM_JOB") || x.ActionType.Contains("AI_REPO") ? "INFO"
                             : "INFO",
                    Message = $"Tiến trình {x.ActionType} đã thực thi.",
                    CreatedAt = x.RecordedAt ?? DateTime.Now,
                    ActionType = x.ActionType,
                    DurationSeconds = x.DurationSeconds,
                    RecordedAt = x.RecordedAt
                })
                .ToListAsync();

            return (200, "Lấy danh sách System Logs thành công.", dbLogs);
        }

        // 3. Hàm cũ: Lấy Health Check của hệ thống
        public async Task<(int StatusCode, string Message, object? Data)> GetSystemHealthAsync()
        {
            bool isDbHealthy = false;
            try { isDbHealthy = await _context.Database.CanConnectAsync(); } catch { }

            bool isAiHealthy = false;
            string aiStatus = "Offline";
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                var stopwatch = Stopwatch.StartNew();
                var response = await client.GetAsync("https://generativelanguage.googleapis.com");
                stopwatch.Stop();

                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    isAiHealthy = true;
                    aiStatus = stopwatch.ElapsedMilliseconds > 2000 ? "Slow Response" : "Online";
                }
            }
            catch { aiStatus = "Connection Error"; }

            bool isHangfireRunning = Hangfire.JobStorage.Current != null;

            var healthData = new
            {
                BackendApi = new { Status = "Online", IsGood = true },
                SqlDatabase = new { Status = isDbHealthy ? "Healthy" : "Disconnected", IsGood = isDbHealthy },
                HangfireQueue = new { Status = isHangfireRunning ? "Running" : "Error", IsGood = isHangfireRunning },
                JobScraper = new { Status = "Active", IsGood = true },
                GeminiAiService = new { Status = aiStatus, IsGood = isAiHealthy }
            };

            return (200, "Lấy System Health thành công.", healthData);
        }

        // 4. Hàm cũ: Tạo Summary bằng AI
        public async Task<(int StatusCode, string Message, object? Data)> GetAiSummaryAsync()
        {
            var latestDate = await _context.TrendAnalyses.MaxAsync(t => (DateOnly?)t.AnalyzedDate);
            if (latestDate == null) return (404, "Chưa có dữ liệu Trend để AI phân tích.", null);

            string cacheKey = $"AiMarketSummary_{latestDate.Value:yyyyMMdd}";

            if (_cache.TryGetValue(cacheKey, out string? cachedSummary))
            {
                return (200, "Lấy tóm tắt AI từ Cache thành công.", new { summary = cachedSummary });
            }

            var topTrends = await _context.TrendAnalyses
                .Where(t => t.AnalyzedDate == latestDate)
                .Join(_context.SkillNodes, t => t.SkillNodeId, n => n.SkillNodeId, (t, n) => new { n.NodeName, t.TrendScore })
                .OrderByDescending(x => x.TrendScore)
                .Take(10)
                .ToListAsync();

            string trendDataString = string.Join(", ", topTrends.Select(x => $"{x.NodeName} ({x.TrendScore} điểm)"));

            string prompt = $"Dưới đây là dữ liệu xu hướng kỹ năng IT mới nhất được hệ thống cào về: {trendDataString}. " +
                            $"Đóng vai trò là một chuyên gia dữ liệu, hãy tóm tắt thị trường này bằng đúng 3 gạch đầu dòng ngắn gọn (không nói dài dòng, không giải thích thêm) cho System Admin xem.";

            string aiResponseText = "";

            try
            {
                var response = await _geminiService.GetChatMessageContentAsync(prompt);
                aiResponseText = response?.Content ?? "";

                if (string.IsNullOrWhiteSpace(aiResponseText))
                {
                    return (500, "AI không trả về nội dung tóm tắt.", null);
                }

                _cache.Set(cacheKey, aiResponseText, TimeSpan.FromHours(24));
            }
            catch (Exception ex)
            {
                return (500, $"Lỗi Semantic Kernel (Gemini): {ex.Message}", null);
            }

            return (200, "AI tạo tóm tắt thành công.", new { summary = aiResponseText });
        }

        // 5. HÀM MỚI BỔ SUNG: Lấy dữ liệu cho trang Admin Giám sát AI (Tokens & Chat History)
        public async Task<(int StatusCode, string Message, object Data)> GetAiMonitorLogsAsync()
        {
            try
            {
                // Dùng Mock Data chuẩn để trả về Giao diện. 
                // Sau này bạn có thể Join với bảng User/ChatHistory thật nếu cần.
                var mockLogs = new List<AiChatLogDto>
                {
                    new AiChatLogDto { LogId = Guid.NewGuid(), StudentName = "Bùi Ngọc Tâm", StudentEmail = "tam@fpt.edu.vn", UserPrompt = "Em muốn học Backend thì bắt đầu từ đâu?", AiResponse = "Chào bạn, để trở thành Backend Dev, bạn cần nắm vững C# cơ bản, SQL Server, và ASP.NET Core...", TokensUsed = 450, CreatedAt = DateTime.Now.AddMinutes(-10), AiModel = "gemini-1.5-pro" },
                    new AiChatLogDto { LogId = Guid.NewGuid(), StudentName = "Huỳnh Công Hòa", StudentEmail = "hoa@fpt.edu.vn", UserPrompt = "Giải thích lỗi Foreign Key giúp em", AiResponse = "Lỗi Foreign Key xảy ra khi bạn cố chèn một giá trị vào bảng con nhưng giá trị đó không tồn tại ở bảng cha...", TokensUsed = 320, CreatedAt = DateTime.Now.AddHours(-2), AiModel = "gemini-1.5-pro" },
                    new AiChatLogDto { LogId = Guid.NewGuid(), StudentName = "Nguyễn Tại Hậu", StudentEmail = "hau@fpt.edu.vn", UserPrompt = "Lộ trình học ReactJS 2026?", AiResponse = "Lộ trình ReactJS 2026: 1. HTML/CSS/JS. 2. React Hooks. 3. Next.js...", TokensUsed = 512, CreatedAt = DateTime.Now.AddDays(-1), AiModel = "gpt-4o-mini" }
                };

                // Tính toán thống kê Token
                var stats = new AiStatsDto
                {
                    TotalRequests = mockLogs.Count,
                    TotalTokensUsed = 1282,
                    EstimatedCostUsd = Math.Round((1282 / 1000.0) * 0.0015, 5)
                };

                var dashboardData = new AdminAiMonitorDashboardDto
                {
                    Stats = stats,
                    ChatLogs = mockLogs
                };

                return (200, "Lấy dữ liệu giám sát AI thành công", dashboardData);
            }
            catch (Exception ex)
            {
                // Task<(int, string, object)> yêu cầu Data không thể null tuỳ ý nếu không cho phép nullable object.
                // Trả về chuỗi rỗng hoặc object rỗng để an toàn.
                return (500, $"Lỗi hệ thống: {ex.Message}", new { });
            }
        }
    }
}