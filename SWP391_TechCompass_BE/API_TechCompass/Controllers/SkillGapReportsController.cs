using Microsoft.AspNetCore.Hosting; // Thêm using này tại Controller
using Microsoft.AspNetCore.Mvc;
using Service_TechCompass.Interfaces;

namespace API_TechCompass.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SkillGapReportsController : ControllerBase
    {
        private readonly ISkillGapReportService _skillGapReportService;
        private readonly IWebHostEnvironment _env; // Inject vào đây

        public SkillGapReportsController(ISkillGapReportService skillGapReportService, IWebHostEnvironment env)
        {
            _skillGapReportService = skillGapReportService;
            _env = env;
        }

        [HttpPost("{studentId}/generate")]
        public async Task<IActionResult> GenerateReport(Guid studentId)
        {
            try
            {
                // Lấy đường dẫn vật lý của wwwroot từ tầng API rồi truyền xuống Service dưới dạng chuỗi string thông thường
                string webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                var result = await _skillGapReportService.GenerateGapReportAsync(studentId, webRootPath);
                return Ok(new
                {
                    Message = "Sinh báo cáo khoảng trống kỹ năng thành công",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}