using System;
using System.Collections.Generic;

namespace Service_TechCompass.DTOs
{
    public class LearningHistoryDto
    {
        public Guid HistoryId { get; set; }
        public string ActionType { get; set; } = null!;
        public int DurationSeconds { get; set; }
        public DateTime RecordedAt { get; set; }
    }

    public class AiInsightDashboardDto
    {
        public List<LearningHistoryDto> Histories { get; set; } = new();
        public string AiWeeklyAdvice { get; set; } = null!;
    }
}