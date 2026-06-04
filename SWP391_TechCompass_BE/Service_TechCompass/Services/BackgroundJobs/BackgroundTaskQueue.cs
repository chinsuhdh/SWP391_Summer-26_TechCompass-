using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Service_TechCompass.DTOs.Background;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services.BackgroundJobs
{
    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly Channel<TelemetryEventDto> _queue;

        public BackgroundTaskQueue(int capacity = 1000)
        {
            // Bounded channel giúp tránh tràn RAM nếu hệ thống bị dội bom quá nhiều request
            var options = new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<TelemetryEventDto>(options);
        }

        public async ValueTask QueueTelemetryEventAsync(TelemetryEventDto telemetryEvent)
        {
            ArgumentNullException.ThrowIfNull(telemetryEvent);
            await _queue.Writer.WriteAsync(telemetryEvent);
        }

        public async ValueTask<TelemetryEventDto> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}