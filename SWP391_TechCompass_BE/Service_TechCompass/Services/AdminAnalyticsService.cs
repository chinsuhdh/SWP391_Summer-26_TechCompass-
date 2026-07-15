using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Repository_TechCompass;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class AdminAnalyticsService : IAdminAnalyticsService
    {
        private readonly Swp391CareerRoadmapContext _context;

        public AdminAnalyticsService(Swp391CareerRoadmapContext context)
        {
            _context = context;
        }

        // 1. Market Analytics: Lấy Top 10 kèm đối chiếu dữ liệu
        public async Task<object> GetMarketAnalyticsAsync()
        {
            // Bước 1: Xác định ngày cào mới nhất và ngày ngay trước đó
            var latestDate = await _context.TrendAnalyses.MaxAsync(t => (DateOnly?)t.AnalyzedDate);

            if (latestDate == null)
            {
                return new { Status = "Success", Data = new List<object>() };
            }

            var previousDate = await _context.TrendAnalyses
                .Where(t => t.AnalyzedDate < latestDate)
                .MaxAsync(t => (DateOnly?)t.AnalyzedDate);

            // Bước 2: Kéo data Hôm nay, Join bảng để lấy Tên và Gom nhóm (Loại bỏ lặp tên)
            var todayRaw = await _context.TrendAnalyses
                .Where(t => t.AnalyzedDate == latestDate)
                .Join(_context.SkillNodes,
                      t => t.SkillNodeId,
                      n => n.SkillNodeId,
                      (t, n) => new { SkillName = n.NodeName, Score = t.TrendScore })
                .ToListAsync();

            var topToday = todayRaw
                .GroupBy(x => x.SkillName)
                .Select(g => new
                {
                    SkillName = g.Key,
                    ScoreToday = g.Max(x => x.Score) // Nếu có nhiều ID trùng tên, lấy điểm cao nhất
                })
                .OrderByDescending(x => x.ScoreToday)
                .Take(10)
                .ToList();

            // Bước 3: Tương tự, kéo data Hôm qua (nếu có)
            var yesterdayRaw = previousDate != null
                ? await _context.TrendAnalyses
                    .Where(t => t.AnalyzedDate == previousDate)
                    .Join(_context.SkillNodes,
                          t => t.SkillNodeId,
                          n => n.SkillNodeId,
                          (t, n) => new { SkillName = n.NodeName, Score = t.TrendScore })
                    .ToListAsync()
                : null;

            // Bước 4: Đối chiếu và ghép data trên RAM bằng C# (Tuyệt đối an toàn, không lo lỗi EF Core)
            var result = topToday.Select(today =>
            {
                // Tìm tất cả các dòng của hôm qua có cùng Tên
                var yesterdayData = yesterdayRaw?.Where(y => y.SkillName == today.SkillName).ToList();

                // Trích xuất điểm (Nếu không có lấy mặc định là 0)
                var scoreYesterday = (yesterdayData != null && yesterdayData.Any())
                                        ? yesterdayData.Max(y => y.Score)
                                        : 0;

                return new
                {
                    SkillName = today.SkillName,
                    ScoreToday = today.ScoreToday,
                    ScoreYesterday = scoreYesterday
                };
            }).ToList();

            return new { Status = "Success", Data = result };
        }

        // 2. Student Activity
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
                        .FullName
                })
                .ToListAsync();

            return new { Status = "Success", Data = recentActivities };
        }

        public async Task<object> GetStudentStatsAsync()
        {
            var today = DateTime.Now.Date;

            // Đếm trực tiếp từ bảng JobPostings (Bảng lưu tin tuyển dụng)
            var jobsToday = await _context.JobPostings
    .Where(j => j.ScrapedAt != null && j.ScrapedAt.Value.Date == today) // Truy cập qua .Value
    .CountAsync();

            return new
            {
                Status = "Success",
                TotalStudents = await _context.Students.CountAsync(),
                TotalJobsScraped = await _context.JobPostings.CountAsync(),
                JobsToday = jobsToday, // React sẽ nhận biến này
                LastScraped = await _context.JobPostings.MaxAsync(j => (DateTime?)j.ScrapedAt)
            };
        }
    }
}