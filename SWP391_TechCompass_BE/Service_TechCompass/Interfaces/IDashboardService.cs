using System;
using System.Threading.Tasks;
using Service_TechCompass.DTOs;

namespace Service_TechCompass.Interfaces
{
    public interface IDashboardService
    {
        // Gọi khi load trang Dashboard
        Task<DashboardOverviewDto> GetOverviewAsync(Guid studentId);

        // Method này dành cho Hangfire gọi ngầm (Background Job)
        Task CalculateAndCacheDashboardMetricsAsync(Guid studentId);
    }
}