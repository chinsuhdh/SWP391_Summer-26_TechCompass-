namespace Service_TechCompass.DTOs
{
    // DTO thống kê số lượng sinh viên theo từng định hướng nghề nghiệp
    public class StudentRoleStatDto
    {
        public int TargetRoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public int StudentCount { get; set; }
    }

    // DTO tổng hợp phân tích lỗ hổng kiến thức của toàn khóa (Cohort)
    public class CohortSkillGapDto
    {
        public int SkillNodeId { get; set; }
        public string SkillNodeName { get; set; } = string.Empty;
        public int MissingStudentCount { get; set; } // Số lượng SV đang bị thiếu kỹ năng này
        public double DeficiencyPercentage { get; set; } // Tỷ lệ % SV bị hụt trên tổng số SV
    }
}