using System;
using System.Collections.Generic;

namespace Repository_TechCompass.Models;

public partial class JobPosting
{
    public Guid PostingId { get; set; }

    public string? SourcePlatform { get; set; }

    public string? JobTitle { get; set; }

    public string? CompanyName { get; set; }

    public string? JobDescriptionRaw { get; set; }

    public DateTime? ScrapedAt { get; set; }

    public virtual ICollection<SkillNode> SkillNodes { get; set; } = new List<SkillNode>();
}
