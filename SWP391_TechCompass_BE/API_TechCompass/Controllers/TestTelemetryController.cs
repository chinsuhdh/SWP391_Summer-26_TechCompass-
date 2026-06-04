using Microsoft.AspNetCore.Mvc;
using Repository_TechCompass;
using Service_TechCompass.Interfaces;

namespace API_TechCompass.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class TestTelemetryController : ControllerBase
    {
        private readonly ITelemetryService _telemetryService;
        private readonly Swp391CareerRoadmapContext _context;

        public TestTelemetryController(ITelemetryService telemetryService, Swp391CareerRoadmapContext context)
        {
            _telemetryService = telemetryService;
            _context = context;
        }

        [HttpPost("fire-event")]
        public async Task<IActionResult> FireTelemetryEvent()
        {
            // Lấy đại 1 tiến độ học tập có sẵn trong DB để làm Foreign Key chuẩn
            var validProgress = _context.RoadmapProgresses.FirstOrDefault();

            if (validProgress == null)
                return BadRequest("Lỗi Test: Bảng 'roadmap_progress' trong Database của bạn đang trống. Hãy Insert tay 1 dòng dữ liệu vào đó trước để lấy Foreign Key nhé!");

            // Gọi service đẩy vào Queue kèm theo ProgressId hợp lệ
            await _telemetryService.LogLearningHistoryAsync(
                studentId: validProgress.StudentId,
                progressId: validProgress.ProgressId,
                actionType: "TEST_WORKER",
                durationSeconds: 120,
                details: "Test thử chức năng Background Queue"
            );

            return Ok(new { message = "Đã ném event vào Queue thành công! Hãy check log Console." });
        }
    }
}