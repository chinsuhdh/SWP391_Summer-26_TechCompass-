using System;
using System.Collections.Generic;

namespace Service_TechCompass.DTOs
{
    // Task 51 & 52: DTO cho Job Matching và Filter
    public class JobFilterDto
    {
        public string? Keyword { get; set; }
        public string? SourcePlatform { get; set; } // "LinkedIn", "TopCV"
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class JobMatchDto
    {
        public Guid PostingId { get; set; }
        public string JobTitle { get; set; } = null!;
        public string CompanyName { get; set; } = null!;
        public string SourcePlatform { get; set; } = null!;
        public decimal MatchPercentage { get; set; }
        public List<string> MatchedSkills { get; set; } = new();
        public List<string> MissingSkills { get; set; } = new();
    }

    // Task 56: DTO cho Biểu đồ Trend
    public class TrendChartDto
    {
        public string NodeName { get; set; } = null!;
        public List<TrendPointDto> DataPoints { get; set; } = new();
    }

    public class TrendPointDto
    {
        public DateTime AnalyzedDate { get; set; }
        public decimal DemandPercent { get; set; }
        public decimal TrendScore { get; set; }
    }
}