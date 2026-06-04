using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Repository_TechCompass;
using Repository_TechCompass.Models;
using Service_TechCompass.Interfaces;


namespace Service_TechCompass.Services.BackgroundJobs
{
    public class TelemetryWorker : BackgroundService
    {
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TelemetryWorker> _logger;
        private const int MAX_RETRIES = 3;

        // Lưu ý: Trong BackgroundService, không thể inject trực tiếp DbContext (vì nó là Scoped).
        // Phải dùng IServiceScopeFactory để tạo scope giả lập.
        public TelemetryWorker(IBackgroundTaskQueue taskQueue, IServiceScopeFactory scopeFactory, ILogger<TelemetryWorker> logger)
        {
            _taskQueue = taskQueue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Telemetry Worker is starting...");

            while (!stoppingToken.IsCancellationRequested)
            {
                var workItem = await _taskQueue.DequeueAsync(stoppingToken);

                try
                {
                    // Tạo một Scope mới để lấy DbContext
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<Swp391CareerRoadmapContext>();

                        var historyRecord = new LearningHistory
                        {
                            HistoryId = workItem.EventId,
                            ActionType = workItem.ActionType,
                            DurationSeconds = workItem.DurationSeconds,
                            RecordedAt = workItem.RecordedAt,

                            ProgressId = workItem.ProgressId // THÊM DÒNG NÀY ĐỂ HẾT LỖI FOREIGN KEY
                        };

                        context.LearningHistories.Add(historyRecord);
                        await context.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation($"Successfully processed telemetry event: {workItem.EventId}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing telemetry event: {workItem.EventId}");

                    // Task 41: Queue retry jobs
                    if (workItem.RetryCount < MAX_RETRIES)
                    {
                        workItem.RetryCount++;
                        _logger.LogWarning($"Retrying event {workItem.EventId} (Attempt {workItem.RetryCount} of {MAX_RETRIES})...");

                        // Đẩy ngược lại vào hàng đợi để xử lý sau
                        await _taskQueue.QueueTelemetryEventAsync(workItem);
                    }
                    else
                    {
                        _logger.LogError($"Event {workItem.EventId} failed after {MAX_RETRIES} retries. Moving to Dead Letter logic.");
                        // Thực tế ở đây có thể ghi vào 1 file log tĩnh hoặc bảng ErrorLog
                    }
                }
            }
        }
    }
}