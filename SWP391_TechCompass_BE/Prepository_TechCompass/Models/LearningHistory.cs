using System;
using System.Collections.Generic;

namespace Repository_TechCompass.Models;

public partial class LearningHistory
{
    public Guid HistoryId { get; set; }

    public Guid? ProgressId { get; set; }

    public string? ActionType { get; set; }

    public int? DurationSeconds { get; set; }

    public DateTime? RecordedAt { get; set; }

    public virtual RoadmapProgress? Progress { get; set; }
}