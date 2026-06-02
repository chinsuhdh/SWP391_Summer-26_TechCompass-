using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service_TechCompass.Interfaces;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace API_TechCompass.Controllers
{
    [Route("api/learning-hub")]
    [ApiController]
    [Authorize] // Yêu cầu sinh viên phải đăng nhập
    public class LearningHubController : ControllerBase
    {
        private readonly ILearningHubService _learningHubService;

        public LearningHubController(ILearningHubService learningHubService)
        {
            _learningHubService = learningHubService;
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return string.IsNullOrEmpty(userIdClaim) ? Guid.Empty : Guid.Parse(userIdClaim);
        }

        // GET: api/learning-hub/nodes/{nodeId}/resources
        // Lấy danh sách video/tài liệu bài học khi click vào một kỹ năng (Node)
        [HttpGet("nodes/{nodeId}/resources")]
        public async Task<IActionResult> GetResourcesByNodeId(int nodeId)
        {
            var userId = GetCurrentUserId();
            var res = await _learningHubService.GetResourcesByNodeIdAsync(userId, nodeId);

            if (res.StatusCode != 200)
            {
                return StatusCode(res.StatusCode, new { message = res.Message });
            }

            return Ok(new { message = res.Message, data = res.Data });
        }
    }
}