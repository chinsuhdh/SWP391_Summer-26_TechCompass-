using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service_TechCompass.Interfaces;
using System;
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
        private readonly ICounselorService _counselorService;

        // GỘP CHUNG 2 SERVICE VÀO 1 CONSTRUCTOR
        public AdminDashboardController(
            IAdminAnalyticsService analyticsService,
            ICounselorService counselorService)
        {
            _analyticsService = analyticsService;
            _counselorService = counselorService;
        }

        // Hàm kiểm tra quyền Admin
        private bool IsAdminUser()
        {
            var roleIdClaim = User.Claims.FirstOrDefault(c => c.Type == "RoleId")?.Value;
            return roleIdClaim == "1" || roleIdClaim == "Admin";
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

        // API của Counselor
        [HttpGet("student-stats")]
        public async Task<IActionResult> GetStudentStats()
        {
            try
            {
                var data = await _counselorService.GetStudentDistributionByRoleAsync();
                return Ok(new { Message = "Lấy dữ liệu phân bổ sinh viên thành công", Data = data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Lỗi hệ thống dashboard", Detail = ex.Message });
            }
        }
    }
}