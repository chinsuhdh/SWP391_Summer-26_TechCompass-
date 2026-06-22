using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Repository_TechCompass;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class SkillGapReportData
    {
        // ĐÃ FIX CẢNH BÁO CS8618: Thêm giá trị mặc định
        public string TargetRoleName { get; set; } = string.Empty;
        public string LatentTalentSummary { get; set; } = string.Empty;
        public List<SkillGapItemDto> GapItems { get; set; } = new List<SkillGapItemDto>();
    }

    public class SkillGapReportService : ISkillGapReportService
    {
        private readonly Swp391CareerRoadmapContext _context;

        public SkillGapReportService(Swp391CareerRoadmapContext context)
        {
            _context = context;
        }

        public async Task<object> GetSkillGapDataAsync(Guid studentId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null || student.TargetRoleId == null)
            {
                return new SkillGapReportData { GapItems = new List<SkillGapItemDto>() };
            }

            var role = await _context.Roles.FindAsync(student.TargetRoleId);
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

            return new SkillGapReportData
            {
                TargetRoleName = targetRoleName,
                LatentTalentSummary = aiSummary,
                GapItems = resultList
            };
        }

        public Task<byte[]> GeneratePdfReportAsync(object reportData)
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

                        // ĐÃ FIX LỖI CS1929: Đưa PaddingTop lên trước .Text()
                        col.Item().PaddingTop(5).Text($"Target Role: {targetRoleName}").FontSize(13).SemiBold();

                        // ĐÃ FIX LỖI CS1929: Đưa PaddingVertical lên trước .LineHorizontal()
                        col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    page.Content().Column(col =>
                    {
                        col.Spacing(15);

                        col.Item().Text("1. AI Profile Summary (Talent & Strengths):")
                                  .FontSize(14).SemiBold().FontColor(Colors.Green.Darken2);

                        col.Item().Text(aiSummary).FontSize(11);

                        // ĐÃ FIX LỖI CS1929: Đưa PaddingTop lên trước .Text()
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

                        // ĐÃ FIX LỖI CS1929: Đưa PaddingTop lên trước .Text()
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

            return Task.FromResult(pdfBytes);
        }
    }
}