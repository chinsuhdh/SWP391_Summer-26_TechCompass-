using System;
using System.Collections.Generic;

namespace Repository_TechCompass.Models;

public partial class SkillNode
{
    public int SkillNodeId { get; set; }

    public int TechPathId { get; set; }

    public int? ParentNodeId { get; set; }

    public string NodeName { get; set; } = null!;

    public string? Description { get; set; }

    public int? PriorityLevel { get; set; }

    public virtual ICollection<SkillNode> InverseParentNode { get; set; } = new List<SkillNode>();

    public virtual ICollection<LearningResource> LearningResources { get; set; } = new List<LearningResource>();

    public virtual SkillNode? ParentNode { get; set; }

    public virtual ICollection<RoadmapProgress> RoadmapProgresses { get; set; } = new List<RoadmapProgress>();

    public virtual ICollection<SkillAssessment> SkillAssessments { get; set; } = new List<SkillAssessment>();

    public virtual TechPath TechPath { get; set; } = null!;

    public virtual ICollection<TrendAnalysis> TrendAnalyses { get; set; } = new List<TrendAnalysis>();

    public virtual ICollection<JobPosting> Postings { get; set; } = new List<JobPosting>();
}
