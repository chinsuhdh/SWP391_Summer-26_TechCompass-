using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service_TechCompass.Interfaces;

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

        [HttpGet("hangfire-stats")]
        public IActionResult GetHangfireStats()
        {
            if (!IsAdminUser()) return Forbid();

            var monitorApi = JobStorage.Current.GetMonitoringApi();

            // Dùng GetStatistics() để lấy toàn bộ dữ liệu thống kê trong 1 lần query (Tối ưu hiệu năng)
            var statistics = monitorApi.GetStatistics();

            var stats = new
            {
                Enqueued = statistics.Enqueued,
                Failed = statistics.Failed,
                Processing = statistics.Processing,
                Succeeded = statistics.Succeeded,
                Scheduled = statistics.Scheduled,
                Servers = statistics.Servers
            };

            // Lấy danh sách 5 job bị lỗi gần nhất để hiển thị cảnh báo
            var failedJobs = monitorApi.FailedJobs(0, 5).Select(x => new {
                JobId = x.Key,
                ExceptionMessage = x.Value.ExceptionMessage,
                FailedAt = x.Value.FailedAt,
                JobName = x.Value.Job?.Method.Name
            }).ToList();

            return Ok(new { Statistics = stats, FailedJobs = failedJobs });
        }

        [HttpGet("system-health")]
        public async Task<IActionResult> GetSystemHealth()
        {
            if (!IsAdminUser()) return Forbid();

            var result = await _monitorService.GetSystemHealthAsync();
            return StatusCode(result.StatusCode, new { message = result.Message, data = result.Data });
        }

        [HttpGet("ai-summary")]
        public async Task<IActionResult> GetAiSummary()
        {
            if (!IsAdminUser()) return Forbid();

            var result = await _monitorService.GetAiSummaryAsync();
            if (result.StatusCode != 200)
                return StatusCode(result.StatusCode, new { message = result.Message });

            return Ok(new { message = result.Message, data = result.Data });
        }

        // =========================================================
        // HÀM MỚI THÊM: LẤY DỮ LIỆU GIÁM SÁT CHI PHÍ VÀ CHẤT LƯỢNG AI
        // =========================================================
        [HttpGet("ai-logs")]
        public async Task<IActionResult> GetAiLogs()
        {
            if (!IsAdminUser()) return Forbid(); // Kế thừa check bảo mật của Admin

            var result = await _monitorService.GetAiMonitorLogsAsync();

            if (result.StatusCode != 200)
                return StatusCode(result.StatusCode, new { message = result.Message });

            // Frontend đang mong đợi object trả về trực tiếp Data nên dùng Ok(result.Data)
            return Ok(result.Data);
        }
    }
}