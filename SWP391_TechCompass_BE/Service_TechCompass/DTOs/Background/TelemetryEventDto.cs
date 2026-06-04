using System;

namespace Service_TechCompass.DTOs.Background
{
    public class TelemetryEventDto
    {
        public Guid EventId { get; set; } = Guid.NewGuid();
        public Guid StudentId { get; set; }

        public Guid ProgressId { get; set; } // THÊM DÒNG NÀY

        public string ActionType { get; set; } = null!;
        public string EventData { get; set; } = string.Empty;
        public int DurationSeconds { get; set; }
        public DateTime RecordedAt { get; set; } = DateTime.Now;
        public int RetryCount { get; set; } = 0;
    }
}