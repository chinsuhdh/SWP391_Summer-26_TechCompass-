using System;
using System.Collections.Generic;

namespace Repository_TechCompass.Models;

public partial class SkillAssessment
{
    public Guid AssessmentId { get; set; }

    public Guid StudentId { get; set; }

    public int SkillNodeId { get; set; }

    public decimal? TestScore { get; set; }

    public string? CodingPatternSnapshot { get; set; }

    public string? AiFeedback { get; set; }

    public DateTime? TakenAt { get; set; }

    public virtual SkillNode SkillNode { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
