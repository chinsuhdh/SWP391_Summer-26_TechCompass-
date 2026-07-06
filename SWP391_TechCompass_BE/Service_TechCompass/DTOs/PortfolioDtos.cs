using System;
using System.Collections.Generic;

namespace Service_TechCompass.DTOs
{
    public class PortfolioDto
    {
        public Guid PortfolioId { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = null!;
        public string? AiProfileSummary { get; set; }
        public string? ShareableUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int AiCareerScore { get; set; }

        // 1. Phân tích độ tương thích nghề nghiệp (Career Suitability & Recommendation)
        public List<CareerSuitabilityDto> CareerSuitabilities { get; set; } = new();
        public CareerRecommendationDto? CareerRecommendation { get; set; }

        // 2. Tài năng ẩn (Latent Talent Analysis)
        public LatentTalentDto? LatentTalent { get; set; }

        // 3. Phân tích khoảng trống kỹ năng (Skill Gap Analysis)
        public SkillGapReportDto? SkillGapAnalysis { get; set; }

        // 4. Tiến độ lộ trình học tập (Learning Progress)
        public RoadmapProgressDto? RoadmapProgress { get; set; }

        // 5. Thành tích học tập từ Transcript (Academic Highlights)
        public AcademicHighlightDto? AcademicHighlights { get; set; }

        // 6. Thống kê tổng quan GitHub (GitHub Statistics)
        public GithubStatsDto? GithubStats { get; set; }

        // 7. Hành trình phát triển (Portfolio Story / Timeline)
        public List<TimelineEventDto> CareerJourney { get; set; } = new();

        // 8. Danh sách Repositories (Đã bổ sung thuộc tính IsFeatured)
        public List<GithubRepoDto> Repositories { get; set; } = new();
    }

    public class GithubRepoDto
    {
        public Guid RepoId { get; set; }
        public string RepoName { get; set; } = null!;
        public string? GithubUrl { get; set; }
        public string? ExtractedTechStack { get; set; }
        public string? AiProjectSummary { get; set; }
        public bool IsFeatured { get; set; } // Phân loại dự án tiêu biểu
        public int DifficultyStars { get; set; } // AI đánh giá độ khó (1-5)
        public DateTime? SyncedAt { get; set; }
    }

    public class CareerSuitabilityDto
    {
        public string RoleName { get; set; } = null!;
        public int MatchPercentage { get; set; }
    }

    public class CareerRecommendationDto
    {
        public string RecommendedRole { get; set; } = null!;
        public int ConfidencePercentage { get; set; }
        public string Reason { get; set; } = null!;
        public List<string> Strengths { get; set; } = new();
        public List<string> Improvements { get; set; } = new();
    }

    public class LatentTalentDto
    {
        public int LogicalThinking { get; set; }
        public int SystemDesign { get; set; }
        public int ProblemSolving { get; set; }
        public int UiUxSense { get; set; }
        public int Communication { get; set; }
    }

    public class SkillGapReportDto
    {
        public string TargetRole { get; set; } = null!;
        public int MatchPercentage { get; set; }
        public List<string> MatchedSkills { get; set; } = new();
        public List<string> MissingSkills { get; set; } = new();
    }

    public class RoadmapProgressDto
    {
        public string RoadmapName { get; set; } = null!;
        public int ProgressPercentage { get; set; }
        public int CompletedNodes { get; set; }
        public int InProgressNodes { get; set; }
        public int RemainingNodes { get; set; }
    }

    public class AcademicHighlightDto
    {
        public decimal Gpa { get; set; }
        public List<string> TopSubjects { get; set; } = new();
        public List<string> WeakSubjects { get; set; } = new();
    }

    public class GithubStatsDto
    {
        public int TotalRepositories { get; set; }
        public int TotalStars { get; set; }
        public int TotalLanguages { get; set; }
        public int TotalCommits { get; set; }
        public string? LastActive { get; set; }
    }

    public class TimelineEventDto
    {
        public string Year { get; set; } = null!;
        public string EventTitle { get; set; } = null!;
        public string Description { get; set; } = null!;
    }

    public class SyncGithubRequestDto
    {
        public string GithubUsername { get; set; } = null!;
    }
}