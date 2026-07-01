using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service_TechCompass.Interfaces;
using System;
using System.Threading.Tasks;

namespace API_TechCompass.Controllers
{
    // Đặt route chuẩn để gom nhóm trên Swagger
    [Route("api/admin/dashboard")]
    [ApiController]
    [Authorize] // Tùy chọn: Thêm Roles = "Admin" nếu muốn khóa chặt quyền
    public class AdminDashboardController : ControllerBase
    {
        private readonly IAdminAnalyticsService _analyticsService;

        public AdminDashboardController(IAdminAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        // API 1: Market Analytics
        [HttpGet("market-analytics")]
        public async Task<IActionResult> GetMarketAnalytics()
        {
            try
            {
                var result = await _analyticsService.GetMarketAnalyticsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // API 2: Student Activity
        [HttpGet("student-activity")]
        public async Task<IActionResult> GetStudentActivity()
        {
            try
            {
                var result = await _analyticsService.GetStudentActivityAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // API 3: Student Stats
        [HttpGet("student-stats")]
        public async Task<IActionResult> GetStudentStats()
        {
            try
            {
                var result = await _analyticsService.GetStudentStatsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}