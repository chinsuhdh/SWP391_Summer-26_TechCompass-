using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;
using Hangfire;

namespace API_TechCompass.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class MarketPulseController : ControllerBase
    {
        private readonly IMarketPulseService _marketPulseService;

        public MarketPulseController(IMarketPulseService marketPulseService)
        {
            _marketPulseService = marketPulseService;
        }

        // ENDPOINT MỚI: Trả về thống kê tổng quan thị trường cho Jobs.jsx
        [HttpGet("overview-stats")]
        public async Task<IActionResult> GetOverviewStats()
        {
            try
            {
                var stats = await _marketPulseService.GetMarketOverviewStatsAsync();
                return Ok(new { message = "Lấy thống kê tổng quan thị trường thành công", data = stats });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // Task 51 & 52: Sinh viên gọi để xem Job hợp với mình
        [HttpPost("students/{studentId}/job-matches")]
        public async Task<IActionResult> GetJobMatches(Guid studentId, [FromBody] JobFilterDto filter)
        {
            try
            {
                var matches = await _marketPulseService.GetMatchingJobsAsync(studentId, filter);
                return Ok(new { message = "Lấy danh sách công việc phù hợp thành công", data = matches });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // Task 56: Lấy dữ liệu cho Trend Dashboard (Chart)
        [HttpGet("trends")]
        public async Task<IActionResult> GetTrends([FromQuery] int days = 30)
        {
            try
            {
                var trends = await _marketPulseService.GetTrendChartDataAsync(days);
                return Ok(new { message = "Thống kê xu hướng kỹ năng", data = trends });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // Task 53, 54, 55: Trigger bằng tay 
        [HttpPost("admin/trigger-scraper")]
        public IActionResult TriggerScraper()
        {
            BackgroundJob.Enqueue<IMarketPulseService>(service => service.RunScraperAndTrendAnalysisAsync());
            return Ok(new { message = "Lệnh cào dữ liệu Job Market đã được đưa vào tiến trình chạy ngầm." });
        }
    }
}