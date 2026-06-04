using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class SkillGapReportService : ISkillGapReportService
    {
        private readonly ISkillGapReportRepository _repository;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        // XÓA BỎ IWebHostEnvironment ở Constructor này
        public SkillGapReportService(ISkillGapReportRepository repository, HttpClient httpClient, IConfiguration configuration)
        {
            _repository = repository;
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<SkillGapReportDto> GenerateGapReportAsync(Guid studentId, string webRootPath)
        {
            var student = await _repository.GetStudentWithSkillsAndTargetAsync(studentId);
            if (student == null || student.TargetRole == null)
                throw new Exception("Không tìm thấy sinh viên hoặc sinh viên chưa chọn Target Career Role.");

            var requiredNodes = student.TargetRole.TechPaths
                                       .SelectMany(tp => tp.SkillNodes)
                                       .Select(n => n.NodeName)
                                       .Distinct().ToList();

            var passedNodes = student.SkillAssessments
                                     .Where(a => a.TestScore >= 5 && a.SkillNode != null)
                                     .Select(a => a.SkillNode.NodeName)
                                     .Distinct().ToList();

            var missingSkills = requiredNodes.Except(passedNodes).ToList();

            string aiSummary = await GenerateAiSummaryAsync(student.TargetRole.RoleName, passedNodes, missingSkills);

            // Truyền trực tiếp webRootPath nhận được từ Controller vào đây
            string fileUrl = await GenerateMockPdfFileAsync(student.StudentId, student.FullName, aiSummary, missingSkills, webRootPath);

            var report = new SkillGapReport
            {
                ReportId = Guid.NewGuid(),
                StudentId = studentId,
                Summary = aiSummary,
                PdfUrl = fileUrl,
                GeneratedAt = DateTime.Now
            };

            await _repository.SaveReportAsync(report);

            return new SkillGapReportDto
            {
                ReportId = report.ReportId,
                Summary = report.Summary,
                PdfUrl = report.PdfUrl,
                GeneratedAt = report.GeneratedAt
            };
        }

        private async Task<string> GenerateAiSummaryAsync(string roleName, List<string> passed, List<string> missing)
        {
            string prompt = $"Sinh viên đang hướng tới vai trò '{roleName}'. " +
                            $"Kỹ năng đã có: {string.Join(", ", passed)}. " +
                            $"Kỹ năng còn thiếu: {string.Join(", ", missing)}. " +
                            $"Hãy viết 1 đoạn văn ngắn (tối đa 4 câu) đánh giá lộ trình và ưu tiên học kỹ năng nào trước để nhanh chóng đáp ứng nhu cầu tuyển dụng.";

            string apiKey = _configuration["GeminiApiConfig:ApiKey"];
            string baseUrl = _configuration["GeminiApiConfig:BaseUrl"];
            string requestUrl = $"{baseUrl}?key={apiKey}";

            var payload = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(requestUrl, payload);
                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsStringAsync();
                    var geminiResponse = JsonSerializer.Deserialize<GeminiResponseDto>(responseData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "Phân tích khoảng trống kỹ năng hoàn tất.";
                }
            }
            catch { }

            return "Hệ thống AI hiện không khả dụng, nhưng bạn có thể xem chi tiết kỹ năng còn thiếu trong file Report.";
        }

        // Nhận thêm biến webRootPath từ hàm gọi
        private async Task<string> GenerateMockPdfFileAsync(Guid studentId, string studentName, string summary, List<string> missingSkills, string webRootPath)
        {
            string folderPath = Path.Combine(webRootPath, "reports");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            string fileName = $"GapReport_{studentId}_{DateTime.Now:yyyyMMddHHmmss}.html";
            string filePath = Path.Combine(folderPath, fileName);

            string htmlContent = $@"
                <h1>Skill Gap Report</h1>
                <h3>Học viên: {studentName}</h3>
                <p><strong>AI Đánh giá:</strong> {summary}</p>
                <h4>Kỹ năng cần bổ sung gấp:</h4>
                <ul>{string.Join("", missingSkills.Select(m => $"<li>{m}</li>"))}</ul>";

            await File.WriteAllTextAsync(filePath, htmlContent);

            return $"/reports/{fileName}";
        }
    }
}