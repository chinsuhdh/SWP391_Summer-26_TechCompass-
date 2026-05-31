using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service_TechCompass.Interfaces;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace API_TechCompass.Controllers
{
    [Route("api/roadmap-engine")]
    [ApiController]
    [Authorize] // Yêu cầu sinh viên phải đăng nhập
    public class RoadmapEngineController : ControllerBase
    {
        private readonly IRoadmapEngineService _engineService;

        public RoadmapEngineController(IRoadmapEngineService engineService)
        {
            _engineService = engineService;
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return string.IsNullOrEmpty(userIdClaim) ? Guid.Empty : Guid.Parse(userIdClaim);
        }

        // POST: api/roadmap-engine/generate
        // API kích hoạt động cơ sinh lộ trình dựa trên Target Role của sinh viên
        [HttpPost("generate")]
        public async Task<IActionResult> GeneratePersonalizedRoadmap()
        {
            var userId = GetCurrentUserId();
            var res = await _engineService.GenerateRoadmapAsync(userId);

            if (res.StatusCode != 200)
            {
                return StatusCode(res.StatusCode, new { message = res.Message });
            }

            return Ok(new { message = res.Message, data = res.Data });
        }
        // GET: api/roadmap-engine/validate/{nodeId}
        // Kiểm tra xem sinh viên có được phép học Node này không
        [HttpGet("validate/{nodeId}")]
        public async Task<IActionResult> ValidatePrerequisite(int nodeId)
        {
            var userId = GetCurrentUserId();
            var res = await _engineService.ValidatePrerequisiteAsync(userId, nodeId);

            if (res.StatusCode != 200 && res.StatusCode != 403)
            {
                return StatusCode(res.StatusCode, new { message = res.Message });
            }

            // Trả về 200 OK kèm theo cờ IsValid (true/false) để UI biết đường hiển thị ổ khóa
            return Ok(new { message = res.Message, isValid = res.IsValid });
        }

        // POST: api/roadmap-engine/recalculate
        // Lưu trữ lộ trình cũ và chạy lại AI Engine để tạo lộ trình mới
        [HttpPost("recalculate")]
        public async Task<IActionResult> RecalculateRoadmap()
        {
            var userId = GetCurrentUserId();
            var res = await _engineService.RecalculateRoadmapAsync(userId);

            if (res.StatusCode != 200)
            {
                return StatusCode(res.StatusCode, new { message = res.Message });
            }

            return Ok(new { message = res.Message, data = res.Data });
        }
    }
}