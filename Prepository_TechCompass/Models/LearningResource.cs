using System;
using System.Collections.Generic;

namespace Repository_TechCompass.Models;

public partial class LearningResource
{
    public int ResourceId { get; set; }

    public int SkillNodeId { get; set; }

    public string Title { get; set; } = null!;

    public string Url { get; set; } = null!;

    public string? ResourceType { get; set; }

    public string? Provider { get; set; }

    public string? DifficultyLevel { get; set; }

    public virtual SkillNode SkillNode { get; set; } = null!;
}
