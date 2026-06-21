// API_TechCompass/Controllers/RoadmapEngineController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service_TechCompass.Interfaces;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System;
using System.Threading.Tasks;

namespace API_TechCompass.Controllers
{
    [Route("api/roadmap-engine")]
    [ApiController]
    [Authorize]
    public class RoadmapEngineController : ControllerBase
    {
        private readonly IRoadmapEngineService _engineService;

        public RoadmapEngineController(IRoadmapEngineService engineService)
        {
            _engineService = engineService;
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return string.IsNullOrEmpty(userIdClaim) ? Guid.Empty : Guid.Parse(userIdClaim);
        }

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

        [HttpGet("validate/{nodeId}")]
        public async Task<IActionResult> ValidatePrerequisite(int nodeId)
        {
            var userId = GetCurrentUserId();
            var res = await _engineService.ValidatePrerequisiteAsync(userId, nodeId);

            if (res.StatusCode != 200 && res.StatusCode != 403)
            {
                return StatusCode(res.StatusCode, new { message = res.Message });
            }

            return Ok(new { message = res.Message, isValid = res.IsValid });
        }

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

        // =========================================================
        // CẬP NHẬT: THÊM TÙY CHỌN [FromQuery] confirmSwitch
        // =========================================================
        [HttpPost("generate-from-session/{sessionId}")]
        public async Task<IActionResult> GenerateRoadmapFromSession(Guid sessionId, [FromQuery] bool confirmSwitch = false)
        {
            var userId = GetCurrentUserId();
            var res = await _engineService.GenerateAiRoadmapFromSessionAsync(userId, sessionId, confirmSwitch);

            // Bắt mã 202 (Yêu cầu xác nhận đổi ngành)
            if (res.StatusCode == 202)
            {
                return StatusCode(202, new { message = res.Message, data = res.Data });
            }

            if (res.StatusCode != 200)
            {
                return StatusCode(res.StatusCode, new { message = res.Message });
            }

            return Ok(new { message = res.Message, data = res.Data });
        }
    }
}