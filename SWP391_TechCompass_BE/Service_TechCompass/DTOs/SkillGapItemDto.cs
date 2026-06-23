namespace Service_TechCompass.DTOs
{
    public class SkillGapItemDto
    {
        public string NodeName { get; set; } = string.Empty;
        public decimal CurrentScore { get; set; }
        public decimal TargetScore { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }
}