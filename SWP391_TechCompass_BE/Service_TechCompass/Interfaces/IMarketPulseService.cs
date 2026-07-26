using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Service_TechCompass.DTOs;

namespace Service_TechCompass.Interfaces
{
    public interface IMarketPulseService
    {
        Task<List<JobMatchDto>> GetMatchingJobsAsync(Guid studentId, JobFilterDto filter);
        Task<(int StatusCode, string Message)> RunScraperAndTrendAnalysisAsync();
        Task<List<TrendChartDto>> GetTrendChartDataAsync(int days = 30);

        Task<object> GetMarketOverviewStatsAsync();
    }
}