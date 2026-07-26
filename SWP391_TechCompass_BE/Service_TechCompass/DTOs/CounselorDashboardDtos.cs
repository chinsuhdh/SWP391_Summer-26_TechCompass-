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

    // DTO cho danh sách sinh viên kèm tiến độ (ĐÃ BỔ SUNG CÁC TRƯỜNG CÒN THIẾU)
    public class CounselorStudentDto
    {
        public Guid StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty; // <-- BỔ SUNG
        public string Email { get; set; } = string.Empty;       // <-- BỔ SUNG
        public string TargetRoleName { get; set; } = string.Empty;
        public double ProgressPercentage { get; set; }
        public string AiScore { get; set; } = "N/A";            // <-- BỔ SUNG
    }

    // DTO cho thống kê bài Assessment
    public class AssessmentStatDto
    {
        public int TotalSessions { get; set; }
        public double AverageScore { get; set; }
        public double PassRate { get; set; } // Tỷ lệ % qua môn
    }

    // DTO cho phân tích Market Alignment
    public class MarketAlignmentDto
    {
        public string SkillName { get; set; } = string.Empty;
        public double MarketDemandPercentage { get; set; } // Độ hot trên thị trường (%)
        public double StudentAdoptionPercentage { get; set; } // Tỷ lệ SV đang học (%)
    }
}