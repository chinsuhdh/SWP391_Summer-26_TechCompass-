using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service_TechCompass.Interfaces;
using System;
using System.Threading.Tasks;

namespace API_TechCompass.Controllers
{
    [Route("api/counselors")]
    [ApiController]
    // [Authorize(Roles = "Counselor, Admin")] // Mở khóa dòng này nếu bạn muốn bảo mật bằng Token
    public class CounselorController : ControllerBase
    {
        private readonly ICounselorService _counselorService;

        public CounselorController(ICounselorService counselorService)
        {
            _counselorService = counselorService;
        }

        [HttpGet("dashboard/assessment-stats")]
        public async Task<IActionResult> GetAssessmentStats()
        {
            try
            {
                var data = await _counselorService.GetAssessmentStatsAsync();
                return Ok(data);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("dashboard/student-stats")]
        public async Task<IActionResult> GetRoleStats()
        {
            try
            {
                var data = await _counselorService.GetStudentDistributionByRoleAsync();
                return Ok(data);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("dashboard/market-alignment")]
        public async Task<IActionResult> GetMarketAlignment()
        {
            try
            {
                var data = await _counselorService.GetMarketAlignmentAsync();
                return Ok(data);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("students")]
        public async Task<IActionResult> GetStudentsProgress([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var data = await _counselorService.GetStudentsProgressAsync(pageNumber, pageSize);
                return Ok(data);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("students/{studentId}/portfolio")]
        public async Task<IActionResult> GetStudentPortfolio(Guid studentId)
        {
            try
            {
                var data = await _counselorService.GetStudentPortfolioAsync(studentId);
                if (data == null) return NotFound(new { message = "Không tìm thấy sinh viên." });

                return Ok(new { data = data }); // Chú ý có chữ `data` thường để khớp với FE
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}