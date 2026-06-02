using Service_TechCompass.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service_TechCompass.Interfaces
{
    public interface IAdminAnalyticsService
    {
        Task<(int StatusCode, string Message, MarketAnalyticsDto? Data)> GetMarketAnalyticsAsync();
        Task<(int StatusCode, string Message, List<StudentActivityDto>? Data)> GetStudentActivityAsync();
    }
}