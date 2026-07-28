using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Repository_TechCompass;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;
using Hangfire;
using System.Security.Claims;
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

        // Task 46, 47, 48: Đồng bộ GitHub — studentId lấy từ JWT, không từ URL
        [HttpPost("sync-github")]
        [Authorize] // [SECURITY-FIX]: Bắt buộc đăng nhập
        public IActionResult SyncGithub([FromBody] SyncGithubRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.GithubUsername))
                return BadRequest(new { message = "Username GitHub không được để trống." });

            // [SECURITY-FIX]: Lấy studentId từ JWT claim — không tin tưởng URL input
            // Student claim "StudentId" được ghi vào token lúc đăng nhập bởi AuthService
            var studentIdClaim = User.FindFirstValue("StudentId");
            if (string.IsNullOrEmpty(studentIdClaim) || !Guid.TryParse(studentIdClaim, out Guid studentId))
                return Unauthorized(new { message = "Không xác định được danh tính sinh viên từ Token." });

            _backgroundJobClient.Enqueue<IPortfolioService>(service =>
                service.ProcessFullGithubPipelineAsync(studentId, request.GithubUsername));

            return Ok(new { message = "Hệ thống đang tiến hành đồng bộ và AI đang phân tích toàn diện hồ sơ năng lực của bạn. Quá trình này có thể mất 1-2 phút!" });
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
        // =========================================================================
        // PHÂN HỆ DÀNH RIÊNG CHO INDUSTRY MENTOR (CỐ VẤN DOANH NGHIỆP)
        // =========================================================================

        // GET: api/Portfolios/all-public
        [HttpGet("all-public")]
        [Authorize(Roles = "Mentor,Admin")] // Cho phép Mentor và Admin truy cập
        public async Task<IActionResult> GetAllPublicPortfolios()
        {
            try
            {
                var portfolios = await _portfolioService.GetAllPublicPortfoliosAsync();
                return Ok(new { Message = "Tải danh sách hồ sơ sinh viên thành công.", Data = portfolios });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Lỗi khi lấy dữ liệu portfolio", Detail = ex.Message });
            }
        }

        // POST: api/Portfolios/{portfolioId}/feedbacks
        [HttpPost("{portfolioId}/feedbacks")]
        [Authorize(Roles = "Mentor")] // CHỈ CHẤP NHẬN MENTOR ĐỂ LẠI ĐÁNH GIÁ THỰC TẾ
        public async Task<IActionResult> LeavePortfolioFeedback(Guid portfolioId, [FromBody] CreatePortfolioFeedbackDto request)
        {
            if (request == null || !ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // LẤY USER ID CỦA MENTOR TRỰC TIẾP TỪ TOKEN (Đảm bảo tính danh chính ngôn thuận, không giả mạo được)
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid mentorUserId))
                {
                    return Unauthorized(new { Error = "Không tìm thấy danh tính Mentor hợp lệ trong Token." });
                }

                // Gọi dịch vụ xử lý lưu thông tin nhận xét
                var feedbackResult = await _portfolioService.AddPortfolioFeedbackAsync(portfolioId, mentorUserId, request);

                return Ok(new
                {
                    Message = "Gửi nhận xét chuyên gia hoàn tất! Sinh viên sẽ nhận được thông báo ngay lập tức.",
                    Data = feedbackResult
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Lỗi hệ thống khi lưu phản hồi", Detail = ex.Message });
            }
        }


    }
}