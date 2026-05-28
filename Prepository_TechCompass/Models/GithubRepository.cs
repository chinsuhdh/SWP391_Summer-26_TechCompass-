using System;
using System.Collections.Generic;

namespace Repository_TechCompass.Models;

public partial class GithubRepository
{
    public Guid RepoId { get; set; }

    public Guid PortfolioId { get; set; }

    public string? RepoName { get; set; }

    public string? GithubUrl { get; set; }

    public string? ReadmeContent { get; set; }

    public string? AiProjectSummary { get; set; }

    public string? ExtractedTechStack { get; set; }

    public DateTime? SyncedAt { get; set; }

    public virtual EPortfolio Portfolio { get; set; } = null!;
}
