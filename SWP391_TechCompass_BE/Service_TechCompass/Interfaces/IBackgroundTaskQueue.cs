using System.Threading;
using System.Threading.Tasks;
using Service_TechCompass.DTOs.Background;

namespace Service_TechCompass.Interfaces
{
    public interface IBackgroundTaskQueue
    {
        ValueTask QueueTelemetryEventAsync(TelemetryEventDto telemetryEvent);
        ValueTask<TelemetryEventDto> DequeueAsync(CancellationToken cancellationToken);
    }
}