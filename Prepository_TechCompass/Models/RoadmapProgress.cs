using System;
using System.Collections.Generic;

namespace Repository_TechCompass.Models;

public partial class RoadmapProgress
{
    public Guid ProgressId { get; set; }

    public Guid StudentId { get; set; }

    public int SkillNodeId { get; set; }

    public string? Status { get; set; }

    public int? CompletionPercent { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<LearningHistory> LearningHistories { get; set; } = new List<LearningHistory>();

    public virtual SkillNode SkillNode { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
