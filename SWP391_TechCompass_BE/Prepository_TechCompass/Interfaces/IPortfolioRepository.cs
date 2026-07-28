// Repository_TechCompass/Interfaces/IPortfolioRepository.cs
using System;
using System.Threading.Tasks;
using Repository_TechCompass.Models;

public interface IPortfolioRepository
{
    Task<EPortfolio?> GetPortfolioByStudentIdAsync(Guid studentId);
    Task<EPortfolio?> GetPortfolioByUrlAsync(string url);
    Task<EPortfolio?> GetPortfolioByIdAsync(Guid portfolioId);
    Task<EPortfolio> CreatePortfolioAsync(EPortfolio portfolio);
    Task UpdatePortfolioAsync(EPortfolio portfolio);
    Task SaveGithubRepoAsync(GithubRepository repo);
    Task<GithubRepository?> GetGithubRepoByIdAsync(Guid repoId);
    Task UpdateGithubRepoAsync(GithubRepository repo);
    Task<bool> SaveFeedbackSessionAsync(MentorSession session);

    // [SECURITY-FIX]: Các method bổ sung để validate chủ sở hữu GitHub username
    /// <summary>Lấy Student theo studentId để đối chiếu GithubUsername phía server.</summary>
    Task<Student?> GetStudentByIdAsync(Guid studentId);

    /// <summary>Cập nhật thông tin Student (dùng để lưu GithubUsername lần đầu sync).</summary>
    Task UpdateStudentAsync(Student student);

    /// <summary>
    /// Xóa toàn bộ GithubRepository thuộc một Portfolio trước khi sync lại.
    /// Ngăn tích lũy dữ liệu pha tạp khi sinh viên đổi username.
    /// </summary>
    Task DeleteAllReposByPortfolioIdAsync(Guid portfolioId);
    Task DeleteGithubRepoAsync(GithubRepository repo);

    /// <summary>
    /// Kiểm tra xem GithubUsername đã bị sinh viên khác sử dụng chưa.
    /// </summary>
    Task<bool> IsGithubUsernameTakenAsync(string githubUsername);
}