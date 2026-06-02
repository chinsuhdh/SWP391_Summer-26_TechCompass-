using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Repository_TechCompass.Repositories
{
    public class AnalyticsRepository : IAnalyticsRepository
    {
        private readonly Swp391CareerRoadmapContext _context;

        public AnalyticsRepository(Swp391CareerRoadmapContext context)
        {
            _context = context;
        }

        public int GetTotalJobPostings()
        {
            return _context.JobPostings.Count();
        }

        public DateTime? GetLastJobScrapedDate()
        {
            return _context.JobPostings.Max(j => j.ScrapedAt);
        }

        public List<TrendAnalysis> GetLatestTrends(int limit)
        {
            // Lấy ra các skill đang có trend cao nhất
            return _context.TrendAnalyses
                           .OrderByDescending(t => t.AnalyzedDate)
                           .ThenByDescending(t => t.TrendScore)
                           .Take(limit).ToList();
        }

        public SkillNode? GetSkillNodeById(int id)
        {
            return _context.SkillNodes.FirstOrDefault(s => s.SkillNodeId == id);
        }

        public List<Student> GetAllStudents()
        {
            return _context.Students.ToList();
        }

        public int CountCompletedNodes(Guid studentId)
        {
            // Đếm số lượng kỹ năng sinh viên đã hoàn thành
            return _context.RoadmapProgresses
                           .Count(r => r.StudentId == studentId && r.Status == "Completed");
        }
    }
}