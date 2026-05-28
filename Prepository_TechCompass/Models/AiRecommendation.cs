using System;
using System.Collections.Generic;

namespace Repository_TechCompass.Models;

public partial class AiRecommendation
{
    public Guid RecommendationId { get; set; }

    public Guid StudentId { get; set; }

    public string? RecommendationType { get; set; }

    public string? ContentJson { get; set; }

    public DateTime? GeneratedAt { get; set; }

    public virtual Student Student { get; set; } = null!;
}
