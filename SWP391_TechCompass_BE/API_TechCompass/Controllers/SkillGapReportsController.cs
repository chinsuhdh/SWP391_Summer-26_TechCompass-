using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Service_TechCompass.Interfaces;

namespace API_TechCompass.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SkillGapReportsController : ControllerBase
    {
        private readonly ISkillGapReportService _reportService;

        public SkillGapReportsController(ISkillGapReportService reportService)
        {
            _reportService = reportService;
        }

        // GET: api/SkillGapReports/{studentId}/generate
        [HttpGet("{studentId}/generate")]
        public async Task<IActionResult> GenerateReportJson(Guid studentId)
        {
            // API hiện tại trả về JSON để Frontend vẽ Chart (FR3.2)
            var reportData = await _reportService.GetSkillGapDataAsync(studentId);
            return Ok(new { Message = "Lấy dữ liệu Skill Gap thành công", Data = reportData });
        }

        // GET: api/SkillGapReports/{studentId}/export-pdf
        [HttpGet("{studentId}/export-pdf")]
        public async Task<IActionResult> ExportPdf(Guid studentId)
        {
            try
            {
                // 1. Lấy dữ liệu phân tích Gap
                var reportData = await _reportService.GetSkillGapDataAsync(studentId);

                // 2. Generate PDF thành mảng byte bằng QuestPDF (Xử lý đồng bộ, không dùng Hangfire)
                byte[] pdfBytes = await _reportService.GeneratePdfReportAsync(reportData);

                // 3. Trả về FileContentResult để Browser tự động tải
                string fileName = $"Skill_Gap_Report_{studentId}_{DateTime.Now:yyyyMMdd}.pdf";

                // Content-Type "application/pdf" chuẩn chỉ cho file PDF
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LỖI EXPORT PDF]: {ex.Message}");
                return StatusCode(500, new { Error = "Không thể tạo file PDF vào lúc này." });
            }
        }
    }
}