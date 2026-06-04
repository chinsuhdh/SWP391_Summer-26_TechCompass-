namespace Service_TechCompass.DTOs
{
    public class SkillGapReportDto
    {
        public Guid ReportId { get; set; }
        public string? Summary { get; set; }
        public string? PdfUrl { get; set; }
        public DateTime? GeneratedAt { get; set; }
    }
}