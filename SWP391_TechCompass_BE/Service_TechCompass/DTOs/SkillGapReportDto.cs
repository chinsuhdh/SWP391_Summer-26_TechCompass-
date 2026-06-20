namespace Service_TechCompass.DTOs
{
    public class SkillGapReportDto
    {
        public Guid ReportId { get; set; }
        public string? Summary { get; set; }
        public string? PdfUrl { get; set; }
        public DateTime? GeneratedAt { get; set; }
    }

    public class SkillGapItemDto
    {
        public string NodeName { get; set; } = string.Empty;
        public decimal CurrentScore { get; set; }
        public decimal TargetScore { get; set; }

        // Bổ sung thêm Target Role Name
        public string RoleName { get; set; } = string.Empty;
    }
}