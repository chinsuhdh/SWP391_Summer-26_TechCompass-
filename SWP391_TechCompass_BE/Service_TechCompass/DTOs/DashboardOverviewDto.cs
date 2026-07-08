using System.Collections.Generic;

namespace Service_TechCompass.DTOs
{
    public class DashboardOverviewDto
    {
        public string TargetRoleName { get; set; } = "Chưa xác định";

        // Đã bổ sung lại thuộc tính bị thiếu
        public string AiQuickSummary { get; set; } = string.Empty;

        public int ReadinessScore { get; set; }
        public int RoadmapScore { get; set; }
        public int MarketMatchScore { get; set; }
        public int PortfolioScore { get; set; }

        public List<CareerFitDto> CareerFits { get; set; } = new();

        public NextActionDto NextAction { get; set; }
        public PortfolioHealthOverviewDto PortfolioHealth { get; set; }
        public MarketPulseSummaryDto MarketPulse { get; set; }
    }

    public class CareerFitDto
    {
        public string RoleName { get; set; } = string.Empty;
        public int MatchPercentage { get; set; }
    }

    public class NextActionDto
    {
        public int NodeId { get; set; }
        public string NodeName { get; set; } = string.Empty;
        public string Difficulty { get; set; } = "Medium";
        public int EstimatedHours { get; set; }
        public List<string> AiReasoningBullets { get; set; } = new();
        public string ExpectedReward { get; set; } = string.Empty;
    }

    public class PortfolioHealthOverviewDto
    {
        public string Architecture { get; set; } = "Missing";
        public string Readme { get; set; } = "Missing";
        public string Testing { get; set; } = "Missing";
        public string AiSuggestion { get; set; } = string.Empty;
        public string EstimatedTime { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
    }

    public class MarketPulseSummaryDto
    {
        public string AiPulseSummary { get; set; } = string.Empty;
    }
}