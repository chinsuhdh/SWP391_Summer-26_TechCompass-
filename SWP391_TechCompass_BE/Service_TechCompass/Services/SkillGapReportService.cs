using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Repository_TechCompass;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class SkillGapReportData
    {
        public string TargetRoleName { get; set; } = string.Empty;
        public string LatentTalentSummary { get; set; } = string.Empty;
        public List<SkillGapItemDto> GapItems { get; set; } = new List<SkillGapItemDto>();
        public Guid StudentId { get; set; }
    }

    public class SkillGapReportService : ISkillGapReportService
    {
        private readonly Swp391CareerRoadmapContext _context;
        private readonly ITelemetryService _telemetryService;
        private readonly IMemoryCache _cache;

        public SkillGapReportService(
            Swp391CareerRoadmapContext context,
            ITelemetryService telemetryService,
            IMemoryCache cache)
        {
            _context = context;
            _telemetryService = telemetryService;
            _cache = cache;
        }

        public async Task<object> GetSkillGapDataAsync(Guid studentId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null || student.TargetRoleId == null)
            {
                return new SkillGapReportData { GapItems = new List<SkillGapItemDto>() };
            }

            var role = await _context.TargetCareerRoles.FindAsync(student.TargetRoleId);

            string targetRoleName = role != null ? role.RoleName : $"Role ID: {student.TargetRoleId}";

            string aiSummary = !string.IsNullOrWhiteSpace(student.LatentTalentSummary)
                                ? student.LatentTalentSummary
                                : "Hệ thống đang thu thập thêm dữ liệu để đưa ra nhận xét chính xác về bạn.";

            var requiredNodes = await (from path in _context.TechPaths
                                       join node in _context.SkillNodes on path.TechPathId equals node.TechPathId
                                       where path.TargetRoleId == student.TargetRoleId
                                       select node).ToListAsync();

            var userSessions = await _context.AssessmentSessions
                .Where(s => s.StudentId == studentId)
                .GroupBy(s => s.SkillNodeId)
                .Select(g => new
                {
                    SkillNodeId = g.Key,
                    MaxScore = g.Max(x => x.TotalQuizScore + x.TotalCodeScore)
                })
                .ToListAsync();

            var resultList = new List<SkillGapItemDto>();

            foreach (var node in requiredNodes)
            {
                var session = userSessions.FirstOrDefault(s => s.SkillNodeId == node.SkillNodeId);
                decimal currentPercent = session != null ? (session.MaxScore / 20.0m) * 100m : 0m;

                resultList.Add(new SkillGapItemDto
                {
                    NodeName = node.NodeName,
                    CurrentScore = Math.Round(currentPercent, 0),
                    TargetScore = 80,
                    RoleName = targetRoleName
                });
            }

            // LOGIC COOLDOWN: Chặn spam log VIEW_SKILL_GAP vào Database
            string cacheKey = $"ViewSkillGapLog_{studentId}";
            if (!_cache.TryGetValue(cacheKey, out _))
            {
                await _telemetryService.LogLearningHistoryAsync(
                    studentId: studentId,
                    progressId: Guid.Empty,
                    actionType: "VIEW_SKILL_GAP",
                    durationSeconds: 0,
                    details: $"Đã kiểm tra hổng kỹ năng cho mục tiêu: {targetRoleName}"
                );

                // Set thời gian chờ là 30 phút. 
                _cache.Set(cacheKey, true, TimeSpan.FromMinutes(30));
            }

            return new SkillGapReportData
            {
                StudentId = studentId,
                TargetRoleName = targetRoleName,
                LatentTalentSummary = aiSummary,
                GapItems = resultList
            };
        }

        public async Task<(int StatusCode, string Message)> SaveDailySkillGapReportAsync(Guid studentId)
        {
            try
            {
                var student = await _context.Students.FindAsync(studentId);
                if (student == null) return (404, "Không tìm thấy hồ sơ sinh viên.");

                string aiSummary = !string.IsNullOrWhiteSpace(student.LatentTalentSummary)
                                    ? student.LatentTalentSummary
                                    : "Hệ thống đang thu thập thêm dữ liệu để đưa ra nhận xét chính xác về bạn.";

                DateTime today = DateTime.Now.Date;

                var todayReport = await _context.SkillGapReports
                    .FirstOrDefaultAsync(r => r.StudentId == studentId
                                           && r.GeneratedAt != null
                                           && r.GeneratedAt.Value.Date == today);

                if (todayReport == null)
                {
                    var newReport = new SkillGapReport
                    {
                        ReportId = Guid.NewGuid(),
                        StudentId = studentId,
                        GeneratedAt = DateTime.Now,
                        Summary = aiSummary,
                        PdfUrl = null
                    };
                    _context.SkillGapReports.Add(newReport);
                    await _context.SaveChangesAsync();

                    return (200, "Đã lưu thành công bản tóm tắt phân tích năng lực hôm nay.");
                }

                return (200, "Báo cáo của hôm nay đã tồn tại, không cần tạo mới.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Lỗi lưu Report]: {ex.Message}");
                return (500, "Lỗi hệ thống khi lưu báo cáo.");
            }
        }

        public async Task<byte[]> GeneratePdfReportAsync(object reportData)
        {
            var wrapper = reportData as SkillGapReportData;
            if (wrapper == null || wrapper.GapItems == null || !wrapper.GapItems.Any())
                throw new ArgumentException("Dữ liệu report không hợp lệ.");

            var data = wrapper.GapItems;
            string targetRoleName = wrapper.TargetRoleName;
            string aiSummary = wrapper.LatentTalentSummary;

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("TECH COMPASS - SKILL GAP ANALYSIS REPORT")
                                  .FontSize(18).SemiBold().FontColor(Colors.Teal.Darken2);
                        col.Item().Text($"Generated Date: {DateTime.Now:yyyy-MM-dd HH:mm}");
                        col.Item().PaddingTop(5).Text($"Target Role: {targetRoleName}").FontSize(13).SemiBold();
                        col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    page.Content().Column(col =>
                    {
                        col.Spacing(15);
                        col.Item().Text("1. AI Profile Summary (Talent & Strengths):")
                                  .FontSize(14).SemiBold().FontColor(Colors.Green.Darken2);
                        col.Item().Text(aiSummary).FontSize(11);

                        col.Item().PaddingTop(10).Text("2. Missing & Improvement Areas:")
                                  .FontSize(14).SemiBold().FontColor(Colors.Orange.Darken2);

                        var missingSkills = data.Where(d => d.CurrentScore < d.TargetScore)
                                                .OrderByDescending(d => d.TargetScore - d.CurrentScore)
                                                .ToList();

                        if (missingSkills.Any())
                        {
                            int index = 1;
                            foreach (var skill in missingSkills)
                            {
                                decimal gapSize = skill.TargetScore - skill.CurrentScore;
                                string priority = gapSize >= 40 ? "HIGH" : gapSize >= 20 ? "MEDIUM" : "LOW";
                                string priorityColor = gapSize >= 40 ? Colors.Red.Medium : gapSize >= 20 ? Colors.Orange.Medium : Colors.Blue.Medium;

                                col.Item().Background(Colors.Grey.Lighten4).Padding(10).Column(innerCol =>
                                {
                                    innerCol.Item().Text(txt =>
                                    {
                                        txt.Span($"{index}. {skill.NodeName} - Priority: ").SemiBold();
                                        txt.Span(priority).SemiBold().FontColor(priorityColor);
                                    });
                                    innerCol.Item().Text($"Current Skill Level: {skill.CurrentScore}% | Market Target: {skill.TargetScore}% -> Gap: {gapSize}%");
                                    innerCol.Item().Text($"Learning Link: http://localhost:5173/dashboard/learning?skill={Uri.EscapeDataString(skill.NodeName)}")
                                                   .FontColor(Colors.Blue.Medium).Underline();
                                });
                                index++;
                            }
                        }
                        else
                        {
                            col.Item().Text("Excellent! You meet all the requirements for your Target Role.").Italic();
                        }

                        col.Item().PaddingTop(20).Text("Recommendation: Focus on HIGH priority skills. Access the Learning Hub links above to start.")
                                  .SemiBold();
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            }).GeneratePdf();

            await _telemetryService.LogLearningHistoryAsync(
                studentId: wrapper.StudentId,
                progressId: Guid.Empty,
                actionType: "EXPORT_PDF_REPORT",
                durationSeconds: 0,
                details: $"Đã xuất PDF báo cáo Skill Gap thành công."
            );

            return pdfBytes;
        }
    }
}