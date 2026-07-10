using Microsoft.EntityFrameworkCore;
using Repository_TechCompass;
using Service_TechCompass.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace Service_TechCompass.Services
{
    public class AdminAnalyticsService : IAdminAnalyticsService
    {
        private readonly Swp391CareerRoadmapContext _context;

        public AdminAnalyticsService(Swp391CareerRoadmapContext context)
        {
            _context = context;
        }

        // 1. Market Analytics: Hiển thị top kỹ năng hot dựa trên TrendAnalysis
        public async Task<object> GetMarketAnalyticsAsync()
        {
            var topSkills = await _context.TrendAnalyses
                .OrderByDescending(t => t.TrendScore)
                .Take(5)
                .Select(t => new {
                    SkillName = _context.SkillNodes.FirstOrDefault(n => n.SkillNodeId == t.SkillNodeId).NodeName,
                    t.TrendScore,
                    t.DemandPercent
                })
                .ToListAsync();

            return new { Status = "Success", Data = topSkills };
        }

        // 2. Student Activity: Hiển thị log hoạt động mới nhất từ LearningHistories
        public async Task<object> GetStudentActivityAsync()
        {
            var recentActivities = await _context.LearningHistories
                .OrderByDescending(h => h.RecordedAt)
                .Take(10)
                .Select(h => new {
                    h.ActionType,
                    h.RecordedAt,
                    StudentName = _context.Students
                        .FirstOrDefault(s => s.RoadmapProgresses.Any(p => p.ProgressId == h.ProgressId))
                        .FullName // Giả định có property FullName
                })
                .ToListAsync();

            return new { Status = "Success", Data = recentActivities };
        }

        // 3. Student Stats: Thống kê tổng quan + Tình trạng Scraping (JobPostings)
        public async Task<object> GetStudentStatsAsync()
        {
            return new
            {
                Status = "Success",
                TotalStudents = await _context.Students.CountAsync(),
                TotalJobsScraped = await _context.JobPostings.CountAsync(),
                LastScraped = await _context.JobPostings.MaxAsync(j => (DateTime?)j.ScrapedAt)
            };
        }
    }
}