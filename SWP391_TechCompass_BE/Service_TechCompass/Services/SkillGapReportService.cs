using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;
// THÊM NAMESPACE SEMANTIC KERNEL
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Service_TechCompass.Services
{
    public class SkillGapReportService : ISkillGapReportService
    {
        private readonly ISkillGapReportRepository _repository;
        private readonly IConfiguration _configuration;
        private readonly IChatCompletionService _chatCompletionService; // Thay thế HttpClient bằng SK

        // Loại bỏ hoàn toàn HttpClient khỏi Constructor
        public SkillGapReportService(ISkillGapReportRepository repository, IConfiguration configuration, Kernel kernel)
        {
            _repository = repository;
            _configuration = configuration;
            // Khởi tạo engine chat completion của Gemini
            _chatCompletionService = kernel.GetRequiredService<IChatCompletionService>("GeminiChat");
        }

        public async Task<SkillGapReportDto> GenerateGapReportAsync(Guid studentId, string webRootPath)
        {
            var student = await _repository.GetStudentWithSkillsAndTargetAsync(studentId);
            if (student == null || student.TargetRole == null)
                throw new Exception("Không tìm thấy sinh viên hoặc sinh viên chưa chọn Target Career Role.");

            var requiredNodes = student.TargetRole.TechPaths
                                       .SelectMany(tp => tp.SkillNodes)
                                       .Select(n => n.NodeName)
                                       .MakeDistinct().ToList();

            var passedNodes = student.SkillAssessments
                                     .Where(a => a.TestScore >= 5 && a.SkillNode != null)
                                     .Select(a => a.SkillNode.NodeName)
                                     .MakeDistinct().ToList();

            var missingSkills = requiredNodes.Except(passedNodes).ToList();

            string aiSummary = await GenerateAiSummaryAsync(student.TargetRole.RoleName, passedNodes, missingSkills);

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
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage("Bạn là chuyên gia phân tích nhân sự và kỹ năng ngành IT (Tech Career Advisor).");

            string prompt = $"Sinh viên đang hướng tới vai trò '{roleName}'. " +
                            $"Kỹ năng đã có: {string.Join(", ", passed)}. " +
                            $"Kỹ năng còn thiếu: {string.Join(", ", missing)}. " +
                            $"Hãy viết 1 đoạn văn ngắn (tối đa 4 câu) bằng tiếng Việt đánh giá lộ trình và ưu tiên học kỹ năng nào trước để nhanh chóng đáp ứng nhu cầu tuyển dụng.";

            chatHistory.AddUserMessage(prompt);

            try
            {
                // Đăng ký gọi kết nối API thông qua lõi kết nối Semantic Kernel
                var response = await _chatCompletionService.GetChatMessageContentAsync(chatHistory);
                return response.ToString() ?? "Phân tích khoảng trống kỹ năng hoàn tất.";
            }
            catch
            {
                return "Hệ thống AI hiện không khả dụng, nhưng bạn có thể xem chi tiết kỹ năng còn thiếu trong file Report.";
            }
        }

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

    // Hàm mở rộng nội bộ hỗ trợ loại bỏ trùng lặp phần tử danh sách nhanh
    internal static class IEnumerableExtensions
    {
        public static IEnumerable<T> MakeDistinct<T>(this IEnumerable<T> source)
        {
            return source.Distinct();
        }
    }
}