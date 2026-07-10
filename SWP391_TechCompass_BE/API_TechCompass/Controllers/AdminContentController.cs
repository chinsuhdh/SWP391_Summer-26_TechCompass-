using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;
using System.Threading.Tasks;

namespace API_TechCompass.Controllers
{
    [Route("api/admin/content")]
    [ApiController]
    [Authorize] // Bắt buộc đăng nhập
    public class AdminContentController : ControllerBase
    {
        private readonly IAdminContentService _contentService;

        public AdminContentController(IAdminContentService contentService)
        {
            _contentService = contentService;
        }

        private bool IsAdminUser()
        {
            var roleIdClaim = User.FindFirst("RoleId")?.Value;
            return roleIdClaim == "1";
        }

        // --- API TECH PATH ---
        [HttpPost("tech-paths")]
        public async Task<IActionResult> CreateTechPath([FromBody] CreateUpdateTechPathDto request)
        {
            if (!IsAdminUser()) return StatusCode(403, new { message = "Không có quyền Admin." });
            var res = await _contentService.CreateTechPathAsync(request);
            return StatusCode(res.StatusCode, new { message = res.Message });
        }

        // --- API SKILL NODE ---
        [HttpGet("skill-nodes")]
        public async Task<IActionResult> GetAllSkillNodes()
        {
            // Endpoint này giúp Frontend lấy danh sách Node đổ vào dropdown
            var res = await _contentService.GetAllSkillNodesAsync();
            return StatusCode(res.StatusCode, new { message = res.Message, data = res.Data });
        }

        [HttpPost("skill-nodes")]
        public async Task<IActionResult> CreateSkillNode([FromBody] CreateUpdateSkillNodeDto request)
        {
            if (!IsAdminUser()) return StatusCode(403, new { message = "Không có quyền Admin." });
            var res = await _contentService.CreateSkillNodeAsync(request);
            return StatusCode(res.StatusCode, new { message = res.Message });
        }

        // --- API LEARNING RESOURCE ---
        [HttpGet("learning-resources")]
        public async Task<IActionResult> GetAllResources(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] int? nodeId = null,
            [FromQuery] string? search = null)
        {
            // API phân trang phục vụ cho việc render danh sách lớn hơn 1000 bản ghi thỏa mãn điều kiện lọc
            var res = await _contentService.GetAllLearningResourcesAsync(page, pageSize, nodeId, search);
            return StatusCode(res.StatusCode, new { message = res.Message, data = res.Data });
        }

        [HttpGet("learning-resources/{id}")]
        public async Task<IActionResult> GetResourceById(int id)
        {
            // Endpoint bổ sung giúp form Edit load lại dữ liệu cũ theo ID tài nguyên
            var res = await _contentService.GetLearningResourceByIdAsync(id);
            return StatusCode(res.StatusCode, new { message = res.Message, data = res.Data });
        }

        [HttpPost("learning-resources")]
        public async Task<IActionResult> CreateResource([FromBody] CreateUpdateLearningResourceDto request)
        {
            if (!IsAdminUser()) return StatusCode(403, new { message = "Không có quyền Admin." });
            var res = await _contentService.CreateLearningResourceAsync(request);
            return StatusCode(res.StatusCode, new { message = res.Message });
        }

        [HttpPut("learning-resources/{id}")]
        public async Task<IActionResult> UpdateResource(int id, [FromBody] CreateUpdateLearningResourceDto request)
        {
            if (!IsAdminUser()) return StatusCode(403, new { message = "Không có quyền Admin." });
            var res = await _contentService.UpdateLearningResourceAsync(id, request);
            return StatusCode(res.StatusCode, new { message = res.Message });
        }

        [HttpDelete("learning-resources/{id}")]
        public async Task<IActionResult> DeleteResource(int id)
        {
            if (!IsAdminUser()) return StatusCode(403, new { message = "Không có quyền Admin." });
            var res = await _contentService.DeleteLearningResourceAsync(id);
            return StatusCode(res.StatusCode, new { message = res.Message });
        }

        // --- API ĐỒNG BỘ ROADMAP TỪ GITHUB ---
        [HttpPost("sync-roadmap")]
        public async Task<IActionResult> SyncRoadmap([FromQuery] string rawUrl, [FromQuery] int targetRoleId)
        {
            if (!IsAdminUser()) return StatusCode(403, new { message = "Không có quyền Admin." });

            if (string.IsNullOrEmpty(rawUrl) || targetRoleId <= 0)
            {
                return BadRequest(new { message = "Thiếu rawUrl hoặc targetRoleId hợp lệ." });
            }

            var res = await _contentService.SyncRoadmapFromGitHubAsync(rawUrl, targetRoleId);

            if (res.StatusCode != 200)
            {
                return StatusCode(res.StatusCode, new { message = res.Message });
            }

            return Ok(new { message = res.Message });
        }
    }
}