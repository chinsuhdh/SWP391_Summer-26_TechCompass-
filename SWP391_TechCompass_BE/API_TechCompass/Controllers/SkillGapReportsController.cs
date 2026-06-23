// src/API_TechCompass/Controllers/SkillGapReportsController.cs
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service_TechCompass.Interfaces;

namespace API_TechCompass.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SkillGapReportsController : ControllerBase
    {
        private readonly ISkillGapReportService _reportService;
        private readonly ICounselorService _counselorService;

        // Inject cả 2 Service vào chung một Constructor duy nhất
        public SkillGapReportsController(
            ISkillGapReportService reportService,
            ICounselorService counselorService)
        {
            _reportService = reportService;
            _counselorService = counselorService;
        }

        // BỔ SUNG: Endpoint lấy dữ liệu JSON Skill Gap hiển thị lên biểu đồ Radar ở Frontend
        [HttpGet("{studentId}/generate")]
        [Authorize]
        public async Task<IActionResult> GenerateReport(Guid studentId)
        {
            try
            {
                // Gọi Service để tính toán dữ liệu
                var reportData = await _reportService.GetSkillGapDataAsync(studentId);

                // Trả về đúng cấu trúc JSON { data: ... } mà Frontend đang bóc tách (response.data?.data)
                return Ok(new
                {
                    Message = "Phân tích thành công",
                    Data = reportData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi khi phân tích dữ liệu: {ex.Message}" });
            }
        }

        // Endpoint 1: Xuất file PDF kết quả Skill Gap của từng sinh viên
        [HttpGet("{studentId}/export-pdf")]
        [Authorize]
        public async Task<IActionResult> ExportPdf(Guid studentId)
        {
            try
            {
                // Gọi đúng tầng Service đã được gán giá trị ở Constructor
                var reportData = await _reportService.GetSkillGapDataAsync(studentId);

                // Generate PDF thành mảng byte bằng QuestPDF (Xử lý đồng bộ, không dùng Hangfire)
                byte[] pdfBytes = await _reportService.GeneratePdfReportAsync(reportData);

                string fileName = $"SkillGapReport_{studentId}_{DateTime.Now:yyyyMMdd}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi tạo PDF: {ex.Message}");
            }
        }

        // Endpoint 2: Phân tích lỗ hổng kiến thức diện rộng diện toàn khóa (Cohort Analysis)
        [HttpGet("cohort-analysis")]
        [Authorize(Roles = "Counselor,Admin")]
        public async Task<IActionResult> GetCohortAnalysis([FromQuery] int top = 3)
        {
            try
            {
                var report = await _counselorService.GetTopCohortSkillGapsAsync(top);
                return Ok(new
                {
                    Message = $"Phân tích thành công Top {top} lỗ hổng kỹ năng nghiêm trọng nhất toàn khóa.",
                    Data = report
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Lỗi khi phân tích dữ liệu khóa học", Detail = ex.Message });
            }
        }
    }
}