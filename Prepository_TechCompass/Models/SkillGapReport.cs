using System;
using System.Collections.Generic;

namespace Repository_TechCompass.Models;

public partial class SkillGapReport
{
    public Guid ReportId { get; set; }

    public Guid StudentId { get; set; }

    public DateTime? GeneratedAt { get; set; }

    public string? Summary { get; set; }

    public string? PdfUrl { get; set; }

    public virtual Student Student { get; set; } = null!;
}
