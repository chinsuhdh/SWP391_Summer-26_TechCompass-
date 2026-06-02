using Repository_TechCompass.Interfaces;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service_TechCompass.Services
{
    public class AdminAnalyticsService : IAdminAnalyticsService
    {
        private readonly IAnalyticsRepository _analyticsRepo;

        public AdminAnalyticsService(IAnalyticsRepository analyticsRepo)
        {
            _analyticsRepo = analyticsRepo;
        }

        public Task<(int StatusCode, string Message, MarketAnalyticsDto? Data)> GetMarketAnalyticsAsync()
        {
            var trends = _analyticsRepo.GetLatestTrends(10); // Lấy top 10 kỹ năng hot nhất
            var trendDtos = trends.Select(t => {
                var skill = _analyticsRepo.GetSkillNodeById(t.SkillNodeId);
                return new SkillTrendDto
                {
                    SkillNodeId = t.SkillNodeId,
                    SkillName = skill != null ? skill.NodeName : "Unknown",
                    TrendScore = t.TrendScore,
                    DemandPercent = t.DemandPercent
                };
            }).ToList();

            var data = new MarketAnalyticsDto
            {
                TotalJobsScraped = _analyticsRepo.GetTotalJobPostings(),
                LastScrapedDate = _analyticsRepo.GetLastJobScrapedDate(),
                TopTrendingSkills = trendDtos
            };

            return Task.FromResult<(int, string, MarketAnalyticsDto?)>((200, "Lấy dữ liệu Market Analytics thành công.", data));
        }

        public Task<(int StatusCode, string Message, List<StudentActivityDto>? Data)> GetStudentActivityAsync()
        {
            var students = _analyticsRepo.GetAllStudents();
            var data = students.Select(s => new StudentActivityDto
            {
                StudentId = s.StudentId,
                FullName = s.FullName,
                StudentCode = s.StudentCode,
                CompletedNodes = _analyticsRepo.CountCompletedNodes(s.StudentId),
                LastUpdatedAt = s.UpdatedAt
            }).ToList();

            return Task.FromResult<(int, string, List<StudentActivityDto>?)>((200, "Lấy dữ liệu giám sát sinh viên thành công.", data));
        }
    }
}