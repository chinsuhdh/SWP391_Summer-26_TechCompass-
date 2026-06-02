using System;

namespace Service_TechCompass.DTOs
{
    public class AiRecommendationDto
    {
        public Guid RecommendationId { get; set; }
        public Guid StudentId { get; set; }
        public string? RecommendationType { get; set; }
        public string? ContentJson { get; set; }
        public DateTime? GeneratedAt { get; set; }
    }

    public class SystemLogDto
    {
        public Guid LogId { get; set; }
        public string LogLevel { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}