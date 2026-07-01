using Service_TechCompass.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service_TechCompass.Interfaces
{
    public interface IAdminAnalyticsService
    {
        Task<object> GetMarketAnalyticsAsync();
        Task<object> GetStudentActivityAsync();
        Task<object> GetStudentStatsAsync();
    }
}