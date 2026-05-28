using System;
using System.Collections.Generic;

namespace Repository_TechCompass.Models;

public partial class TechPath
{
    public int TechPathId { get; set; }

    public int TargetRoleId { get; set; }

    public string? PathName { get; set; }

    public string? Description { get; set; }

    public int? TotalNodes { get; set; }

    public virtual ICollection<SkillNode> SkillNodes { get; set; } = new List<SkillNode>();

    public virtual TargetCareerRole TargetRole { get; set; } = null!;
}
