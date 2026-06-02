using Service_TechCompass.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service_TechCompass.Interfaces
{
    public interface IAdminMonitorService
    {
        Task<(int StatusCode, string Message, List<AiRecommendationDto>? Data)> GetAllAiRecommendationsAsync();
        Task<(int StatusCode, string Message, List<SystemLogDto>? Data)> GetSystemLogsAsync();
    }
}