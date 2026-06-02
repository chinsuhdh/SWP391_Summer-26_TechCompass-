using System;
using System.Collections.Generic;

namespace Service_TechCompass.DTOs
{
    // --- DTO CHO MARKET ANALYTICS ---
    public class SkillTrendDto
    {
        public int SkillNodeId { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public decimal? TrendScore { get; set; }
        public decimal? DemandPercent { get; set; }
    }

    public class MarketAnalyticsDto
    {
        public int TotalJobsScraped { get; set; }
        public DateTime? LastScrapedDate { get; set; }
        public List<SkillTrendDto> TopTrendingSkills { get; set; } = new List<SkillTrendDto>();
    }

    // --- DTO CHO STUDENT ACTIVITY ---
    public class StudentActivityDto
    {
        public Guid StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? StudentCode { get; set; }
        public int CompletedNodes { get; set; } // Số lượng node kỹ năng đã hoàn thành
        public DateTime? LastUpdatedAt { get; set; }
    }
}