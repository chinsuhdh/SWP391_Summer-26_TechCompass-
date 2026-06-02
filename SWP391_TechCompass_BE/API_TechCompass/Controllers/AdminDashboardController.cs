using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service_TechCompass.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace API_TechCompass.Controllers
{
    [Route("api/admin/dashboard")]
    [ApiController]
    [Authorize] // Bắt buộc đăng nhập
    public class AdminDashboardController : ControllerBase
    {
        private readonly IAdminAnalyticsService _analyticsService;

        public AdminDashboardController(IAdminAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        private bool IsAdminUser()
        {
            var roleIdClaim = User.Claims.FirstOrDefault(c => c.Type == "RoleId")?.Value;
            return roleIdClaim == "1"; // Kiểm tra quyền Admin
        }

        [HttpGet("market-analytics")]
        public async Task<IActionResult> GetMarketAnalytics()
        {
            if (!IsAdminUser()) return Forbid();

            var result = await _analyticsService.GetMarketAnalyticsAsync();
            if (result.StatusCode != 200)
                return StatusCode(result.StatusCode, new { message = result.Message });
            return Ok(new { message = result.Message, data = result.Data });
        }

        [HttpGet("student-activity")]
        public async Task<IActionResult> GetStudentActivity()
        {
            if (!IsAdminUser()) return Forbid();

            var result = await _analyticsService.GetStudentActivityAsync();
            if (result.StatusCode != 200)
                return StatusCode(result.StatusCode, new { message = result.Message });
            return Ok(new { message = result.Message, data = result.Data });
        }
    }
}