using System;

namespace Service_TechCompass.DTOs
{
    public class DashboardOverviewDto
    {
        public string TargetRoleName { get; set; } = "Chưa xác định";
        public int ReadinessScore { get; set; }
        public string AiQuickSummary { get; set; } = string.Empty;

        public NextActionDto NextAction { get; set; }
        public AssessmentSuggestionDto Assessment { get; set; }
    }

    public class NextActionDto
    {
        public int NodeId { get; set; }
        public string NodeName { get; set; } = string.Empty;
        public int EstimatedHours { get; set; }
        public string AiReasoning { get; set; } = string.Empty;
    }

    public class AssessmentSuggestionDto
    {
        public string CompletedTopic { get; set; } = string.Empty;
        public bool IsMockInterviewAvailable { get; set; }
    }
}