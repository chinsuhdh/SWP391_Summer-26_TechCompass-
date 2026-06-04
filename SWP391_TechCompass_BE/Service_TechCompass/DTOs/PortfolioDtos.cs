using System;
using System.Collections.Generic;

namespace Service_TechCompass.DTOs
{
    public class PortfolioDto
    {
        public Guid PortfolioId { get; set; }
        public Guid StudentId { get; set; }
        public string? AiProfileSummary { get; set; }
        public string? ShareableUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
        public List<GithubRepoDto> Repositories { get; set; } = new();
    }

    public class GithubRepoDto
    {
        public Guid RepoId { get; set; }
        public string RepoName { get; set; } = null!;
        public string? GithubUrl { get; set; }
        public string? ExtractedTechStack { get; set; }
        public string? AiProjectSummary { get; set; }
        public DateTime? SyncedAt { get; set; }
    }

    public class SyncGithubRequestDto
    {
        public string GithubUsername { get; set; } = null!;
        // Nếu muốn lấy cả private repo, bạn có thể truyền thêm Personal Access Token (PAT) vào đây.
        // Tạm thời mình dùng Username để lấy Public Repos cho đơn giản.
    }
}