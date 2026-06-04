using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;

namespace API_TechCompass.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PortfoliosController : ControllerBase
    {
        private readonly IPortfolioService _portfolioService;

        public PortfoliosController(IPortfolioService portfolioService)
        {
            _portfolioService = portfolioService;
        }

        // Task 44: Lấy Portfolio của sinh viên đang login
        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetMyPortfolio(Guid studentId)
        {
            var portfolio = await _portfolioService.GetPortfolioAsync(studentId);
            if (portfolio == null) return NotFound(new { message = "Portfolio chưa được khởi tạo." });
            return Ok(portfolio);
        }

        // Dành cho nhà tuyển dụng xem E-Portfolio qua Link public
        [HttpGet("shared")]
        public async Task<IActionResult> GetPortfolioByUrl([FromQuery] string url)
        {
            var portfolio = await _portfolioService.GetPortfolioByUrlAsync(url);
            if (portfolio == null) return NotFound(new { message = "Đường dẫn Portfolio không tồn tại." });
            return Ok(portfolio);
        }

        // Task 45: Tạo link chia sẻ
        [HttpPost("{studentId}/generate-url")]
        public async Task<IActionResult> GenerateShareableUrl(Guid studentId)
        {
            var url = await _portfolioService.GenerateShareableUrlAsync(studentId);
            return Ok(new { message = "Tạo URL thành công", url = url });
        }

        // Task 46, 47, 48: Đồng bộ GitHub
        [HttpPost("{studentId}/sync-github")]
        public async Task<IActionResult> SyncGithub(Guid studentId, [FromBody] SyncGithubRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.GithubUsername))
                return BadRequest(new { message = "Username GitHub không được để trống." });

            var result = await _portfolioService.SyncGithubReposAsync(studentId, request.GithubUsername);
            if (result.StatusCode != 200) return StatusCode(result.StatusCode, new { message = result.Message });
            return Ok(new { message = result.Message });
        }

        // Task 49, 50: Phân tích dự án bằng AI
        [HttpPost("repos/{repoId}/analyze")]
        public async Task<IActionResult> AnalyzeRepository(Guid repoId)
        {
            var result = await _portfolioService.AnalyzeRepoWithAiAsync(repoId);
            if (result.StatusCode != 200) return StatusCode(result.StatusCode, new { message = result.Message });
            return Ok(new { message = result.Message });
        }
    }
}