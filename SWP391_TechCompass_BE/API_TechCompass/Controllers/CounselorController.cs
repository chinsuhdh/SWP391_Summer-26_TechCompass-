using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service_TechCompass.Interfaces;
using System;
using System.Threading.Tasks;

namespace API_TechCompass.Controllers
{
    [ApiController]
    [Route("api/counselors")]
    [Authorize(Roles = "Counselor, Admin")]
    public class CounselorController : ControllerBase
    {
        private readonly ICounselorService _counselorService;

        public CounselorController(ICounselorService counselorService)
        {
            _counselorService = counselorService;
        }

        [HttpGet("dashboard/student-stats")]
        public async Task<IActionResult> GetStudentStats()
        {
            try
            {
                var result = await _counselorService.GetStudentDistributionByRoleAsync();
                if (result == null || result.Count == 0) return Ok(new { message = "Chưa có dữ liệu." });
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thống kê sinh viên.", error = ex.Message });
            }
        }

        [HttpGet("skill-gaps/cohort-analysis")]
        public async Task<IActionResult> GetCohortAnalysis([FromQuery] int topCount = 5)
        {
            try
            {
                if (topCount <= 0) return BadRequest("Tham số topCount phải > 0.");
                var result = await _counselorService.GetTopCohortSkillGapsAsync(topCount);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi phân tích lỗ hổng kỹ năng.", error = ex.Message });
            }
        }

        // ================= CÁC API MỚI =================

        /// <summary>
        /// Lấy danh sách tiến độ học tập của sinh viên (Có phân trang và Lọc)
        /// API: GET /api/counselors/students?pageNumber=1&pageSize=10&roleId=1
        /// </summary>
        [HttpGet("students")]
        public async Task<IActionResult> GetStudentsProgress([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] int? roleId = null)
        {
            try
            {
                if (pageNumber <= 0 || pageSize <= 0) return BadRequest("Phân trang không hợp lệ.");
                var result = await _counselorService.GetStudentsProgressAsync(pageNumber, pageSize, roleId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách sinh viên.", error = ex.Message });
            }
        }

        /// <summary>
        /// Thống kê tỷ lệ hoàn thành bài test Assessment
        /// API: GET /api/counselors/dashboard/assessment-stats
        /// </summary>
        [HttpGet("dashboard/assessment-stats")]
        public async Task<IActionResult> GetAssessmentStats()
        {
            try
            {
                var result = await _counselorService.GetAssessmentStatsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi thống kê Assessment.", error = ex.Message });
            }
        }

        /// <summary>
        /// Phân tích độ vênh giữa kỹ năng thị trường cần và kỹ năng sinh viên đang học
        /// API: GET /api/counselors/dashboard/market-alignment
        /// </summary>
        [HttpGet("dashboard/market-alignment")]
        public async Task<IActionResult> GetMarketAlignment()
        {
            try
            {
                var result = await _counselorService.GetMarketAlignmentAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi phân tích Market Alignment.", error = ex.Message });
            }
        }
    }
}