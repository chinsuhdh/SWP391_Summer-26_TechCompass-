using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;

namespace Repository_TechCompass.Repositories
{
    public class MarketPulseRepository : IMarketPulseRepository
    {
        private readonly Swp391CareerRoadmapContext _context;

        public MarketPulseRepository(Swp391CareerRoadmapContext context)
        {
            _context = context;
        }

        public async Task<Student?> GetStudentWithPassedSkillsAsync(Guid studentId)
        {
            return await _context.Students
                .Include(s => s.SkillAssessments)
                .ThenInclude(sa => sa.SkillNode)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);
        }

        public async Task<List<JobPosting>> GetJobPostingsAsync(string? keyword, string? source, int skip, int take)
        {
            var query = _context.JobPostings.Include(j => j.SkillNodes).AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(j => j.JobTitle.Contains(keyword) || j.CompanyName.Contains(keyword));

            if (!string.IsNullOrEmpty(source))
                query = query.Where(j => j.SourcePlatform == source);

            return await query.OrderByDescending(j => j.ScrapedAt).Skip(skip).Take(take).ToListAsync();
        }

        public async Task SaveJobPostingAsync(JobPosting job)
        {
            _context.JobPostings.Add(job);
            await _context.SaveChangesAsync();
        }

        public async Task SaveTrendAnalysisAsync(List<TrendAnalysis> trends)
        {
            _context.TrendAnalyses.AddRange(trends);
            await _context.SaveChangesAsync();
        }

        public async Task<List<TrendAnalysis>> GetTrendsForChartAsync(DateTime fromDate)
        {
            // Convert DateTime sang DateOnly để so sánh với Database
            DateOnly fromDateOnly = DateOnly.FromDateTime(fromDate);

            return await _context.TrendAnalyses
                .Include(t => t.SkillNode)
                .Where(t => t.AnalyzedDate >= fromDateOnly)
                .OrderBy(t => t.AnalyzedDate)
                .ToListAsync();
        }

        public async Task<List<SkillNode>> GetAllSkillNodesAsync()
        {
            return await _context.SkillNodes.ToListAsync();
        }
    }
}