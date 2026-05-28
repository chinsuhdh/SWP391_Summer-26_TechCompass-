using System;
using System.Collections.Generic;

namespace Repository_TechCompass.Models;

public partial class TrendAnalysis
{
    public int AnalysisId { get; set; }

    public int SkillNodeId { get; set; }

    public decimal? TrendScore { get; set; }

    public decimal? DemandPercent { get; set; }

    public DateOnly? AnalyzedDate { get; set; }

    public virtual SkillNode SkillNode { get; set; } = null!;
}
