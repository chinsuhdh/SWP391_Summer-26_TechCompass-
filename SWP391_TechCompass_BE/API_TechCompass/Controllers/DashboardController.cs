using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Service_TechCompass.Interfaces;
using Hangfire;

namespace API_TechCompass.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("{studentId}/overview")]
        public async Task<IActionResult> GetDashboardOverview(Guid studentId)
        {
            try
            {
                var result = await _dashboardService.GetOverviewAsync(studentId);

                // Trả về định dạng bọc object thống nhất giống các API khác của bạn
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // Tùy chọn: API Endpoint để force trigger job bằng tay (dành cho Admin/Testing)
        [HttpPost("{studentId}/trigger-metrics-job")]
        public IActionResult TriggerDashboardMetricsJob(Guid studentId)
        {
            // Hangfire sẽ xếp hàng tác vụ này để chạy ngầm ngay lập tức
            BackgroundJob.Enqueue<IDashboardService>(service => service.CalculateAndCacheDashboardMetricsAsync(studentId));

            return Accepted(new { success = true, message = "Đã đưa tác vụ phân tích AI vào hàng đợi (Hangfire)." });
        }
    }
}