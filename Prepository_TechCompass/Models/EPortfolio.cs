using System;
using System.Collections.Generic;

namespace Repository_TechCompass.Models;

public partial class EPortfolio
{
    public Guid PortfolioId { get; set; }

    public Guid StudentId { get; set; }

    public string? ShareableUrl { get; set; }

    public string? AiProfileSummary { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<GithubRepository> GithubRepositories { get; set; } = new List<GithubRepository>();

    public virtual Student Student { get; set; } = null!;
}
