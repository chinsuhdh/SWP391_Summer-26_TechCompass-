using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;
using Repository_TechCompass;

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

        // ==========================================
        // 1. API CHÍNH DÀNH CHO FRONTEND
        // ==========================================
        public async Task<DashboardOverviewDto> GetOverviewAsync(Guid studentId)
        {
            var result = new DashboardOverviewDto();

            // FIX 1: Dùng _context.Students thay vì _context.StudentProfiles
            var profile = await _context.Students
                .Include(p => p.TargetRole)
                .FirstOrDefaultAsync(p => p.StudentId == studentId);

            result.TargetRoleName = profile?.TargetRole?.RoleName ?? "Chưa xác định";

            // 2. Lấy Next Best Action (Node tiếp theo chưa học)
            var nextNode = await _context.RoadmapProgresses
                .Include(p => p.SkillNode)
                .Where(p => p.StudentId == studentId && p.Status != "Completed")
                .OrderBy(p => p.SkillNode.PriorityLevel)
                .Select(p => p.SkillNode)
                .FirstOrDefaultAsync();

            if (nextNode != null)
            {
                result.NextAction = new NextActionDto
                {
                    NodeId = nextNode.SkillNodeId,
                    NodeName = nextNode.NodeName,
                    
                    EstimatedHours = 4,
                    AiReasoning = $"Dữ liệu Market Pulse cho thấy {nextNode.NodeName} đang là kỹ năng cốt lõi cho {result.TargetRoleName}."
                };
            }

            // 3. Lấy Topic vừa hoàn thành để gợi ý làm test
            var lastCompleted = await _context.RoadmapProgresses
                .Include(p => p.SkillNode)
                .Where(p => p.StudentId == studentId && p.Status == "Completed")
                .OrderByDescending(p => p.UpdatedAt)
                .Select(p => p.SkillNode.NodeName)
                .FirstOrDefaultAsync();

            result.Assessment = new AssessmentSuggestionDto
            {
                CompletedTopic = lastCompleted ?? "Kiến thức nền tảng",
                IsMockInterviewAvailable = !string.IsNullOrEmpty(lastCompleted)
            };

            // 4. LẤY DỮ LIỆU TỪ CACHE (Do Hangfire tính toán ngầm)
            string cacheKey = $"DashboardMetrics_{studentId}";
            if (_cache.TryGetValue(cacheKey, out DashboardOverviewDto cachedMetrics))
            {
                result.ReadinessScore = cachedMetrics.ReadinessScore;
                result.AiQuickSummary = cachedMetrics.AiQuickSummary;
            }
            else
            {
                // Fallback nếu Cache trống
                result.ReadinessScore = 0;
                result.AiQuickSummary = "Hệ thống AI đang phân tích dữ liệu của bạn, vui lòng quay lại sau vài phút.";
            }

            return result;
        }

        // ==========================================
        // 2. HANGFIRE BACKGROUND JOB (Chạy ngầm)
        // ==========================================
        public async Task CalculateAndCacheDashboardMetricsAsync(Guid studentId)
        {
            // 1. Tính Readiness Score
            var allRoadmapNodes = await _context.RoadmapProgresses
                .Include(p => p.SkillNode)
                .Where(p => p.StudentId == studentId)
                .ToListAsync();

            if (!allRoadmapNodes.Any()) return;

            int completedCount = allRoadmapNodes.Count(p => p.Status == "Completed");
            int readinessScore = (int)Math.Round((double)completedCount / allRoadmapNodes.Count * 100);

            // 2. Lấy danh sách kỹ năng để nhúng vào Prompt
            var completedSkills = allRoadmapNodes
                .Where(p => p.Status == "Completed")
                .Select(p => p.SkillNode.NodeName)
                .ToList();

            var missingSkills = allRoadmapNodes
                .Where(p => p.Status != "Completed")
                .Take(3) // Lấy top 3 cái thiếu
                .Select(p => p.SkillNode.NodeName)
                .ToList();

            string skillsStr = string.Join(", ", completedSkills);
            string missingStr = string.Join(", ", missingSkills);

            // 3. Gọi Gemini sinh ra câu Summary ngắn gọn
            string prompt = $@"
Bạn là AI Mentor. Một sinh viên đang có điểm Readiness Score là {readinessScore}%.
Điểm mạnh (đã học): {skillsStr}. 
Đang thiếu: {missingStr}.
Hãy viết MỘT câu duy nhất (dưới 30 chữ) nhận xét và động viên sinh viên này, tập trung vào hành động tiếp theo.
KHÔNG dùng bullet points, KHÔNG chào hỏi.";

            string aiSummary = string.Empty;
            try
            {
                var chatHistory = new ChatHistory();
                chatHistory.AddUserMessage(prompt);
                var response = await _geminiService.GetChatMessageContentAsync(chatHistory);
                aiSummary = response.ToString().Replace("\"", "").Trim();
            }
            catch
            {
                aiSummary = "Tiếp tục duy trì tiến độ học tập để đạt chuẩn thị trường nhé!";
            }

            // 4. Lưu vào Cache (Set TTL khoảng 1 ngày)
            var metricsToCache = new DashboardOverviewDto
            {
                ReadinessScore = readinessScore,
                AiQuickSummary = aiSummary
            };

            string cacheKey = $"DashboardMetrics_{studentId}";
            _cache.Set(cacheKey, metricsToCache, TimeSpan.FromHours(24));
        }
    }
}