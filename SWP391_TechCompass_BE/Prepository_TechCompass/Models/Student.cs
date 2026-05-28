using System;
using System.Collections.Generic;

namespace Repository_TechCompass.Models;

public partial class Student
{
    public Guid StudentId { get; set; }

    public Guid UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string? StudentCode { get; set; }

    public int? TargetRoleId { get; set; }

    public string? LatentTalentSummary { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<AiChatSession> AiChatSessions { get; set; } = new List<AiChatSession>();

    public virtual ICollection<AiRecommendation> AiRecommendations { get; set; } = new List<AiRecommendation>();

    public virtual EPortfolio? EPortfolio { get; set; }

    public virtual ICollection<MentorSession> MentorSessions { get; set; } = new List<MentorSession>();

    public virtual ICollection<RoadmapProgress> RoadmapProgresses { get; set; } = new List<RoadmapProgress>();

    public virtual ICollection<SkillAssessment> SkillAssessments { get; set; } = new List<SkillAssessment>();

    public virtual ICollection<SkillGapReport> SkillGapReports { get; set; } = new List<SkillGapReport>();

    public virtual TargetCareerRole? TargetRole { get; set; }

    public virtual User User { get; set; } = null!;
}
