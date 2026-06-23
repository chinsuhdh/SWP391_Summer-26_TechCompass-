using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repository_TechCompass;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;
using Hangfire; // ĐÃ THÊM: Dùng cho tiến trình nền

namespace API_TechCompass.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PortfoliosController : ControllerBase
    {
        private readonly IPortfolioService _portfolioService;
        private readonly IBackgroundJobClient _backgroundJobClient; // ĐÃ THÊM: Sử dụng Interface chuẩn của Hangfire

        // ĐÃ THÊM: Tiêm IBackgroundJobClient vào Constructor
        public PortfoliosController(IPortfolioService portfolioService, IBackgroundJobClient backgroundJobClient)
        {
            _portfolioService = portfolioService;
            _backgroundJobClient = backgroundJobClient;
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
        public IActionResult SyncGithub(Guid studentId, [FromBody] SyncGithubRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.GithubUsername))
                return BadRequest(new { message = "Username GitHub không được để trống." });

            // ĐÃ SỬA: Gọi biến _backgroundJobClient thay vì gọi class tĩnh
            _backgroundJobClient.Enqueue<IPortfolioService>(service => service.SyncGithubReposAsync(studentId, request.GithubUsername));

            // Trả về cho Frontend ngay lập tức
            return Ok(new { message = "Hệ thống đang tiến hành tải và phân tích dự án GitHub của bạn dưới nền. Quá trình này có thể mất vài phút. Vui lòng quay lại kiểm tra sau!" });
        }

        // Task 49, 50: Phân tích dự án bằng AI
        [HttpPost("repos/{repoId}/analyze")]
        public IActionResult AnalyzeRepository(Guid repoId)
        {
            // ĐÃ SỬA: Gọi biến _backgroundJobClient thay vì gọi class tĩnh
            _backgroundJobClient.Enqueue<IPortfolioService>(service => service.AnalyzeRepoWithAiAsync(repoId));

            // Trả về ngay lập tức
            return Ok(new { message = "Yêu cầu AI phân tích Repository đã được đưa vào tiến trình chạy ngầm. Quá trình này sẽ hoàn tất sau ít phút." });
        }
    }
}