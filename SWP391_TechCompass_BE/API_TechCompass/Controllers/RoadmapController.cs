using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace API_TechCompass.Controllers
{
    [Route("api/roadmap")]
    [ApiController]
    [Authorize] // Bắt buộc sinh viên đăng nhập
    public class RoadmapController : ControllerBase
    {
        private readonly IRoadmapService _roadmapService;

        public RoadmapController(IRoadmapService roadmapService)
        {
            _roadmapService = roadmapService;
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return string.IsNullOrEmpty(userIdClaim) ? Guid.Empty : Guid.Parse(userIdClaim);
        }

        // DASHBOARD: Hiển thị tiến độ, Next skill, Trends
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var userId = GetCurrentUserId();
            var res = await _roadmapService.GetStudentDashboardAsync(userId);
            if (res.StatusCode != 200) return StatusCode(res.StatusCode, new { message = res.Message });
            return Ok(new { message = res.Message, data = res.Data });
        }

        // TECH PATH MAP: Lấy cây kỹ năng (Frontend gọi API này để vẽ và Zoom roadmap)
        [HttpGet("skill-tree")]
        public async Task<IActionResult> GetSkillTree()
        {
            var userId = GetCurrentUserId();
            var res = await _roadmapService.GetSkillTreeAsync(userId);
            if (res.StatusCode != 200) return StatusCode(res.StatusCode, new { message = res.Message });
            return Ok(new { message = res.Message, data = res.Data });
        }

        // TECH PATH MAP: Đánh dấu hoàn thành Node
        [HttpPost("complete-node")]
        public async Task<IActionResult> MarkNodeCompleted([FromBody] MarkNodeCompletedDto request)
        {
            var userId = GetCurrentUserId();
            var res = await _roadmapService.MarkNodeCompletedAsync(userId, request);
            return StatusCode(res.StatusCode, new { message = res.Message });
        }
    }
}