using System;
using System.Collections.Generic;

namespace Repository_TechCompass.Models;

public partial class TargetCareerRole
{
    public int TargetRoleId { get; set; }

    public string RoleName { get; set; } = null!;

    public string? Description { get; set; }

    public decimal? MarketDemandIndex { get; set; }

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    public virtual ICollection<TechPath> TechPaths { get; set; } = new List<TechPath>();
}
