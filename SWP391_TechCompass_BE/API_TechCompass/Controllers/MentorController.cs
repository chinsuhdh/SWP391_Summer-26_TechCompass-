using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API_TechCompass.Controllers
{
    [ApiController]
    [Route("api/mentors")]
    public class MentorController : ControllerBase
    {
        private readonly IMentorService _mentorService;
        private readonly IPortfolioService _portfolioService; // Inject IPortfolioService để dùng AI

        public MentorController(IMentorService mentorService, IPortfolioService portfolioService)
        {
            _mentorService = mentorService;
            _portfolioService = portfolioService;
        }

        /// <summary>
        /// Lấy danh sách Portfolio công khai (Dành cho Mentor duyệt)
        /// API: GET /api/mentors/portfolios?pageNumber=1&pageSize=10
        /// </summary>
        [HttpGet("portfolios")]
        [Authorize(Roles = "Mentor, Admin")] // Cả Admin và Mentor đều xem được
        public async Task<IActionResult> GetPublicPortfolios([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _mentorService.GetPublicPortfoliosAsync(pageNumber, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách Portfolio.", error = ex.Message });
            }
        }

        /// <summary>
        /// Mentor gửi nhận xét (Feedback) cho một Portfolio
        /// API: POST /api/mentors/portfolios/{portfolioId}/feedbacks
        /// </summary>
        [HttpPost("portfolios/{portfolioId}/feedbacks")]
        [Authorize(Roles = "Mentor")] // CHỈ Mentor mới có quyền thả feedback
        public async Task<IActionResult> SubmitFeedback(Guid portfolioId, [FromBody] SubmitFeedbackRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.ReviewNotes))
                    return BadRequest(new { message = "Nội dung nhận xét không được để trống." });

                // Lấy UserId của Mentor từ JWT Token
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(userIdString, out Guid mentorUserId))
                    return Unauthorized(new { message = "Token không hợp lệ." });

                var isSuccess = await _mentorService.SubmitPortfolioFeedbackAsync(mentorUserId, portfolioId, request.ReviewNotes);

                if (isSuccess)
                    return Ok(new { message = "Đã gửi nhận xét thành công!" });

                return BadRequest(new { message = "Không thể gửi nhận xét lúc này." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi gửi nhận xét.", error = ex.Message });
            }
        }

        /// <summary>
        /// Gợi ý nhận xét bằng AI cho Mentor
        /// API: GET /api/mentors/portfolios/{portfolioId}/ai-suggestion
        /// </summary>
        [HttpGet("portfolios/{portfolioId}/ai-suggestion")]
        [Authorize(Roles = "Mentor")]
        public async Task<IActionResult> GenerateAiSuggestion(Guid portfolioId)
        {
            try
            {
                // Gọi hàm AI đã viết bên PortfolioService
                var aiDraft = await _portfolioService.GenerateAiFeedbackSuggestionAsync(portfolioId);

                // Trả về dạng JSON có chứa nội dung AI sinh ra
                return Ok(new { suggestion = aiDraft });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tạo gợi ý AI.", error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy lịch sử nhận xét của Mentor
        /// API: GET /api/mentors/history
        /// </summary>
        [HttpGet("history")]
        [Authorize(Roles = "Mentor")]
        public async Task<IActionResult> GetFeedbackHistory()
        {
            try
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(userIdString, out Guid mentorUserId))
                    return Unauthorized(new { message = "Token không hợp lệ." });

                var result = await _mentorService.GetMentorFeedbackHistoryAsync(mentorUserId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống.", error = ex.Message });
            }
        }
    }
}