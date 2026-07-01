using System.ComponentModel.DataAnnotations;

namespace Service_TechCompass.DTOs
{
    // --- DTO CHO LẤY DỮ LIỆU (GET) ---
    public class TechPathDto
    {
        public int TechPathId { get; set; }
        public int TargetRoleId { get; set; }
        public string PathName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? TotalNodes { get; set; }
    }

    public class SkillNodeDto
    {
        public int SkillNodeId { get; set; }
        public int TechPathId { get; set; }
        public int? ParentNodeId { get; set; }
        public string NodeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? PriorityLevel { get; set; }

        // --- BỔ SUNG ĐỂ FIX LỖI CHO ROADMAP ---
        public int NodeId { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsLocked { get; set; }

        // --- BỔ SUNG CHO NGHIỆP VỤ MARKET PULSE (TRENDING) ---
        public bool IsTrending { get; set; }
        public decimal? CurrentTrendScore { get; set; }
    }

    public class LearningResourceDto
    {
        public int ResourceId { get; set; }
        public int SkillNodeId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? ResourceType { get; set; }
        public string? Provider { get; set; }
        public string? DifficultyLevel { get; set; }
    }

    // --- DTO CHO CREATE / UPDATE ---
    public class CreateUpdateTechPathDto
    {
        [Required] public string PathName { get; set; } = string.Empty;
        public string? Description { get; set; }
        [Required] public int TargetRoleId { get; set; }
    }

    public class CreateUpdateSkillNodeDto
    {
        [Required] public string NodeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        [Required] public int PathId { get; set; } // Map với TechPathId trong DB
        public int? ParentNodeId { get; set; }
        public int? EstimatedDays { get; set; } // Map với PriorityLevel trong DB
    }

    public class CreateUpdateLearningResourceDto
    {
        [Required] public int NodeId { get; set; } // Map với SkillNodeId trong DB
        [Required] public string Title { get; set; } = string.Empty;
        [Required] public string Url { get; set; } = string.Empty;
        public string? ResourceType { get; set; }
        public string? Provider { get; set; }
        public string? DifficultyLevel { get; set; }
    }
}