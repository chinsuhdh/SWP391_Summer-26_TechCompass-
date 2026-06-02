using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service_TechCompass.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace API_TechCompass.Controllers
{
    [Route("api/admin/monitor")]
    [ApiController]
    [Authorize] // Bắt buộc đăng nhập
    public class AdminMonitorController : ControllerBase
    {
        private readonly IAdminMonitorService _monitorService;

        public AdminMonitorController(IAdminMonitorService monitorService)
        {
            _monitorService = monitorService;
        }

        private bool IsAdminUser()
        {
            var roleIdClaim = User.Claims.FirstOrDefault(c => c.Type == "RoleId")?.Value;
            return roleIdClaim == "1"; // 1 là ID của Role Admin
        }

        [HttpGet("ai-recommendations")]
        public async Task<IActionResult> GetAiRecommendations()
        {
            if (!IsAdminUser()) return Forbid(); // Chỉ Admin được xem

            var result = await _monitorService.GetAllAiRecommendationsAsync();
            if (result.StatusCode != 200)
                return StatusCode(result.StatusCode, new { message = result.Message });
            return Ok(new { message = result.Message, data = result.Data });
        }

        [HttpGet("system-logs")]
        public async Task<IActionResult> GetSystemLogs()
        {
            if (!IsAdminUser()) return Forbid(); // Chỉ Admin được xem

            var result = await _monitorService.GetSystemLogsAsync();
            if (result.StatusCode != 200)
                return StatusCode(result.StatusCode, new { message = result.Message });
            return Ok(new { message = result.Message, data = result.Data });
        }
    }
}