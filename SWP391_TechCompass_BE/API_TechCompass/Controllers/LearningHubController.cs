// API_TechCompass/Controllers/LearningHubController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service_TechCompass.Interfaces;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System;
using System.Threading.Tasks;

namespace API_TechCompass.Controllers
{
    [Route("api/learning-hub")]
    [ApiController]
    [Authorize]
    public class LearningHubController : ControllerBase
    {
        private readonly ILearningHubService _learningHubService;

        public LearningHubController(ILearningHubService learningHubService)
        {
            _learningHubService = learningHubService;
        }

        private Guid GetCurrentUserId()
        {
            // ... (Giữ nguyên hàm GetCurrentUserId của bạn)
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                           ?? User.FindFirstValue("id")
                           ?? User.FindFirstValue("userId");

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                Console.WriteLine("CẢNH BÁO: Không thể lấy được User ID từ Token!");
                return Guid.Empty;
            }
            return userId;
        }

        [HttpGet("nodes/{nodeId}/resources")]
        public async Task<IActionResult> GetResourcesByNodeId(int nodeId)
        {
            // ... (Giữ nguyên)
            var userId = GetCurrentUserId();
            var res = await _learningHubService.GetResourcesByNodeIdAsync(userId, nodeId);
            if (res.StatusCode != 200) return StatusCode(res.StatusCode, new { message = res.Message });
            return Ok(new { message = res.Message, data = res.Data });
        }

        [HttpPost("resources/{resourceId}/enroll")]
        public async Task<IActionResult> EnrollResource(int resourceId)
        {
            // ... (Giữ nguyên)
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return Unauthorized(new { message = "Không xác định được danh tính người dùng." });

            var res = await _learningHubService.EnrollResourceAsync(userId, resourceId);
            if (res.StatusCode != 200 && res.StatusCode != 201) return StatusCode(res.StatusCode, new { message = res.Message });
            return Ok(new { message = res.Message });
        }

        // ==========================================
        // CẬP NHẬT: ĐỔI ROUTE TỪ roadmap/{techPathId} THÀNH my-roadmap
        // ==========================================
        [HttpGet("my-roadmap")]
        public async Task<IActionResult> GetMyRoadmap()
        {
            var userId = GetCurrentUserId();
            var res = await _learningHubService.GetMyRoadmapAsync(userId);

            if (res.StatusCode == 400)
            {
                // Bắn flag requiresOnboarding = true để Frontend biết tự động chuyển sang trang Chọn Ngành
                return BadRequest(new { message = res.Message, requiresOnboarding = true });
            }

            if (res.StatusCode != 200)
            {
                return StatusCode(res.StatusCode, new { message = res.Message });
            }

            return Ok(new { message = res.Message, data = res.Data });
        }
    }
}