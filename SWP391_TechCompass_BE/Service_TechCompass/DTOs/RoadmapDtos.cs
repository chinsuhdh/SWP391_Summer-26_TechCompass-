namespace Service_TechCompass.DTOs

{
    // DTO trả về kết quả sau khi Engine tạo xong lộ trình
    public class GenerateRoadmapResponseDto
    {
        public int TargetRoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public List<SkillNodeDto> GeneratedNodes { get; set; } = new List<SkillNodeDto>();
    }
    // DTO cho Cây kỹ năng (Tech Path Map)
    public class SkillNodeDto
    {
        public int NodeId { get; set; }
        public string NodeName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int? ParentNodeId { get; set; } // Dùng để Frontend vẽ sơ đồ cây và Zoom
        public bool IsCompleted { get; set; } // Đã học xong chưa?
        public bool IsLocked { get; set; } // Khóa nếu Node cha chưa hoàn thành
    }

    // DTO cho Trend Công nghệ
    public class TechTrendDto
    {
        public string TrendName { get; set; } = string.Empty;
        public string TrendDescription { get; set; } = string.Empty;
        public int PopularityScore { get; set; } // Điểm độ hot
    }

    // DTO Tổng hợp cho Dashboard
    public class DashboardSummaryDto
    {
        public int TotalNodes { get; set; }
        public int CompletedNodes { get; set; }
        public double ProgressPercentage { get; set; }
        public SkillNodeDto? NextSkill { get; set; } // Kỹ năng tiếp theo cần học
        public List<TechTrendDto> TechTrends { get; set; } = new List<TechTrendDto>();
    }

    // DTO để nhận request đánh dấu hoàn thành Node
    public class MarkNodeCompletedDto
    {
        public int NodeId { get; set; }
    }
}