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

            // Bước 4: Đối chiếu và ghép data trên RAM bằng C#
            var result = topToday.Select(today =>
            {
                var yesterdayData = yesterdayRaw?.Where(y => y.SkillName == today.SkillName).ToList();

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

        // =========================================================
        // [BUG-012 FIX]: LOẠI BỎ SUB-QUERY TRONG SELECT (TRIỆT CẢNH N+1)
        // =========================================================
        public async Task<object> GetStudentActivityAsync()
        {
            var recentActivities = await _context.LearningHistories
                .Include(h => h.Progress)
                    .ThenInclude(p => p.Student) // Explicit Eager Loading thông qua Relationship
                .OrderByDescending(h => h.RecordedAt)
                .Take(10)
                .Select(h => new
                {
                    h.ActionType,
                    h.RecordedAt,
                    StudentName = h.Progress != null && h.Progress.Student != null
                        ? h.Progress.Student.FullName
                        : "Hệ thống"
                })
                .ToListAsync();

            return new { Status = "Success", Data = recentActivities };
        }

        public async Task<object> GetStudentStatsAsync()
        {
            var today = DateTime.Now.Date;

            // Đếm trực tiếp từ bảng JobPostings
            var jobsToday = await _context.JobPostings
                .Where(j => j.ScrapedAt != null && j.ScrapedAt.Value.Date == today)
                .CountAsync();

            return new
            {
                Status = "Success",
                TotalStudents = await _context.Students.CountAsync(),
                TotalJobsScraped = await _context.JobPostings.CountAsync(),
                JobsToday = jobsToday,
                LastScraped = await _context.JobPostings.MaxAsync(j => (DateTime?)j.ScrapedAt)
            };
        }
    }
}