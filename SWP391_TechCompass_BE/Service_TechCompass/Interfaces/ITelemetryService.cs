using System;
using System.Threading.Tasks;

namespace Service_TechCompass.Interfaces
{
    public interface ITelemetryService
    {
        // Thêm tham số progressId vào hàm
        Task LogLearningHistoryAsync(Guid studentId, Guid progressId, string actionType, int durationSeconds, string details = "");
    }
}