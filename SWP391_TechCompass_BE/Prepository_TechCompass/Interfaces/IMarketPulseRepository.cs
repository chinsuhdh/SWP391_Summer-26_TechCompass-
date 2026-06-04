using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Repository_TechCompass.Models;

namespace Repository_TechCompass.Interfaces
{
    public interface IMarketPulseRepository
    {
        Task<Student?> GetStudentWithPassedSkillsAsync(Guid studentId);
        Task<List<JobPosting>> GetJobPostingsAsync(string? keyword, string? source, int skip, int take);
        Task SaveJobPostingAsync(JobPosting job);
        Task SaveTrendAnalysisAsync(List<TrendAnalysis> trends);
        Task<List<TrendAnalysis>> GetTrendsForChartAsync(DateTime fromDate);
        Task<List<SkillNode>> GetAllSkillNodesAsync();
    }
}