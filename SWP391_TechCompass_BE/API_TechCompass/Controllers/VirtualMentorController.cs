using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service_TechCompass.DTOs.VirtualMentor;
using Service_TechCompass.Interfaces;
using Repository_TechCompass.Interfaces;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace API_TechCompass.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class VirtualMentorController : ControllerBase
    {
        private readonly IVirtualMentorService _mentorService;
        private readonly IUserRepository _userRepo;

        public VirtualMentorController(IVirtualMentorService mentorService, IUserRepository userRepo)
        {
            _mentorService = mentorService;
            _userRepo = userRepo;
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                           ?? User.FindFirstValue("studentId")
                           ?? User.FindFirstValue("StudentId");

            if (string.IsNullOrEmpty(userIdClaim))
            {
                throw new Exception("Không thể trích xuất ID người dùng từ Token. Vui lòng đăng nhập lại.");
            }

            return Guid.Parse(userIdClaim);
        }

        // 1. API MỚI: Trả về danh sách các cuộc trò chuyện cho thanh Sidebar
        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions()
        {
            try
            {
                var userId = GetCurrentUserId();
                var student = _userRepo.GetStudentByUserId(userId);
                if (student == null)
                {
                    return BadRequest(new { Error = "Không tìm thấy hồ sơ sinh viên tương ứng." });
                }

                var sessions = await _mentorService.GetUserSessionsAsync(student.StudentId);
                return Ok(new { Data = sessions });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // 2. CẬP NHẬT API CHAT: Nhận SessionId từ React và trả về SessionId
        [HttpPost("chat")]
        public async Task<IActionResult> ChatWithMentor([FromBody] VirtualMentorChatRequestDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var student = _userRepo.GetStudentByUserId(userId);
                if (student == null)
                {
                    return BadRequest(new { Error = "Không tìm thấy hồ sơ sinh viên tương ứng với tài khoản này." });
                }

                // Gọi Service đã nâng cấp (truyền thêm request.SessionId)
                var result = await _mentorService.ChatAsync(student.StudentId, request.UserMessage, request.SessionId);

                // Trả về kèm SessionId để React biết đang ở luồng chat nào
                return Ok(new { Message = "Thành công", SessionId = result.SessionId, AiResponse = result.AiResponse });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // 3. CẬP NHẬT API LỊCH SỬ: Lấy chi tiết đoạn chat theo SessionId (Route param)
        [HttpGet("chat-history/{sessionId}")]
        public async Task<IActionResult> GetChatHistoryBySession(Guid sessionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var student = _userRepo.GetStudentByUserId(userId);
                if (student == null)
                {
                    return BadRequest(new { Error = "Không tìm thấy hồ sơ sinh viên tương ứng." });
                }

                // Truyền sessionId xuống Service
                var history = await _mentorService.GetChatHistoryAsync(sessionId);
                return Ok(new { Data = history });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // 4. API Fallback: Xử lý mượt mà khi React vô tình gọi thiếu param
        [HttpGet("chat-history")]
        public IActionResult GetChatHistoryFallback()
        {
            return Ok(new { Data = new List<ChatHistoryResponseDto>() });
        }
    }
}