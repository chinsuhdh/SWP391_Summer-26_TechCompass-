using System;
using System.Collections.Generic;

namespace Service_TechCompass.DTOs
{
    // DTO cho hàm GetStudentsProgressAsync
    public class CounselorStudentDto
    {
        public Guid StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string TargetRoleName { get; set; } = string.Empty;
        public int ProgressPercentage { get; set; }
        public int AiScore { get; set; }
    }

    // LƯU Ý: ĐÃ XÓA CLASS PagedResult<T> Ở ĐÂY ĐỂ TRÁNH TRÙNG LẶP

    // DTO cho AssessmentStats
    public class AssessmentStatDto
    {
        public int TotalSessions { get; set; }
        public double PassRate { get; set; }
        public double AverageScore { get; set; }
    }

    // DTO cho StudentRoleStat
    public class StudentRoleStatDto
    {
        public string RoleName { get; set; } = string.Empty;
        public int StudentCount { get; set; }
    }

    // DTO cho MarketAlignment
    public class MarketAlignmentDto
    {
        public string SkillName { get; set; } = string.Empty;
        public double MarketDemandPercentage { get; set; }
        public double StudentAdoptionPercentage { get; set; }
    }

    // DTO cho Portfolio (Chi tiết sinh viên)
    public class StudentPortfolioDto
    {
        public string StudentName { get; set; } = string.Empty;
        public int AiCareerScore { get; set; }
        public string AiProfileSummary { get; set; } = string.Empty;
        public object CareerRecommendation { get; set; } = null!;
        public object SkillGapAnalysis { get; set; } = null!;
        public object RoadmapProgress { get; set; } = null!;
        public object GithubStats { get; set; } = null!;
        public List<object> Repositories { get; set; } = new List<object>();
    }
}