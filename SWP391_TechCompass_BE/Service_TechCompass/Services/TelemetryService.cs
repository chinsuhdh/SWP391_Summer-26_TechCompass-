using System;
using System.Threading.Tasks;
using Service_TechCompass.DTOs.Background;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class TelemetryService : ITelemetryService
    {
        private readonly IBackgroundTaskQueue _taskQueue;

        public TelemetryService(IBackgroundTaskQueue taskQueue)
        {
            _taskQueue = taskQueue;
        }

        public async Task LogLearningHistoryAsync(Guid studentId, Guid progressId, string actionType, int durationSeconds, string details = "")
        {
            var telemetryEvent = new TelemetryEventDto
            {
                StudentId = studentId,
                ProgressId = progressId, // MAPPING VÀO DTO
                ActionType = actionType,
                DurationSeconds = durationSeconds,
                EventData = details
            };

            await _taskQueue.QueueTelemetryEventAsync(telemetryEvent);
        }
    }
}