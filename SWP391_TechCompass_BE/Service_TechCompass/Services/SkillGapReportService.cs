// src/Service_TechCompass/Services/SkillGapReportService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Repository_TechCompass;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace Service_TechCompass.Services
{
    // ĐÃ THÊM: Class bọc dữ liệu để truyền được cả Summary và Danh sách môn học sang PDF
    public class SkillGapReportData
    {
        public string TargetRoleName { get; set; }
        public string LatentTalentSummary { get; set; }
        public List<SkillGapItemDto> GapItems { get; set; }
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

            // 1. LẤY TÊN TARGET ROLE VÀ LỜI KHUYÊN TỪ AI
            var role = await _context.Roles.FindAsync(student.TargetRoleId);
            string targetRoleName = role != null ? role.RoleName : $"Role ID: {student.TargetRoleId}";

            // Lấy nhận xét từ AI, nếu chưa có thì gán chuỗi mặc định
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

            // ĐÃ SỬA: Trả về Object bọc thay vì chỉ trả về List
            return new SkillGapReportData
            {
                TargetRoleName = targetRoleName,
                LatentTalentSummary = aiSummary,
                GapItems = resultList
            };
        }

        // HÀM HỖ TRỢ: Tự động xuống dòng cho đoạn văn dài trong PDF
        private List<string> SplitTextIntoLines(string text, int maxCharsPerLine)
        {
            var words = text.Split(' ');
            var lines = new List<string>();
            var currentLine = "";

            foreach (var word in words)
            {
                if ((currentLine + word).Length > maxCharsPerLine)
                {
                    lines.Add(currentLine.Trim());
                    currentLine = "";
                }
                currentLine += word + " ";
            }
            if (!string.IsNullOrWhiteSpace(currentLine)) lines.Add(currentLine.Trim());
            return lines;
        }

        public async Task<byte[]> GeneratePdfReportAsync(object reportData)
        {
            var wrapper = reportData as SkillGapReportData;
            if (wrapper == null || wrapper.GapItems == null || !wrapper.GapItems.Any())
                throw new ArgumentException("Dữ liệu report không hợp lệ.");

            var data = wrapper.GapItems;
            string targetRoleName = wrapper.TargetRoleName;
            string aiSummary = wrapper.LatentTalentSummary;

            PdfDocumentBuilder builder = new PdfDocumentBuilder();
            PdfPageBuilder page = builder.AddPage(PageSize.A4);

            // ---------------------------------------------------------
            // ĐÃ SỬA LỖI CRASH: Load Font Arial từ Windows hỗ trợ Unicode
            // ---------------------------------------------------------
            var font = builder.AddTrueTypeFont(System.IO.File.ReadAllBytes(@"C:\Windows\Fonts\arial.ttf"));
            var fontBold = builder.AddTrueTypeFont(System.IO.File.ReadAllBytes(@"C:\Windows\Fonts\arialbd.ttf"));
            var fontOblique = builder.AddTrueTypeFont(System.IO.File.ReadAllBytes(@"C:\Windows\Fonts\ariali.ttf"));
            // ---------------------------------------------------------

            // Vẽ Header & Ngữ cảnh Mục tiêu
            page.AddText("TECH COMPASS - SKILL GAP ANALYSIS REPORT", 16, new PdfPoint(50, 750), fontBold);
            page.AddText($"Generated Date: {DateTime.Now:yyyy-MM-dd HH:mm}", 10, new PdfPoint(50, 730), font);
            page.AddText($"Target Role: {targetRoleName}", 12, new PdfPoint(50, 715), fontBold);
            page.AddText("--------------------------------------------------------------------------------", 12, new PdfPoint(50, 700), font);

            int currentY = 670;

            // [MỤC 1] LỜI KHUYÊN TỪ HỆ THỐNG (AI SUMMARY)
            page.AddText("1. AI Profile Summary (Talent & Strengths):", 14, new PdfPoint(50, currentY), fontBold);
            currentY -= 20;

            // Cắt đoạn văn thành nhiều dòng, mỗi dòng khoảng 85 ký tự để vừa khổ A4
            var summaryLines = SplitTextIntoLines(aiSummary, 85);
            foreach (var line in summaryLines)
            {
                page.AddText(line, 11, new PdfPoint(50, currentY), font);
                currentY -= 15;
            }

            currentY -= 15; // Khoảng cách giữa 2 phần

            // [MỤC 2] DANH SÁCH KỸ NĂNG CÒN THIẾU
            page.AddText("2. Missing & Improvement Areas:", 14, new PdfPoint(50, currentY), fontBold);
            currentY -= 30;

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

                    string line1 = $"{index}. {skill.NodeName} - Priority: {priority}";
                    string line2 = $"    Current Skill Level: {skill.CurrentScore}% | Market Target: {skill.TargetScore}% -> Gap: {gapSize}%";
                    string line3 = $"    Learning Link: http://localhost:5173/dashboard/learning?skill={Uri.EscapeDataString(skill.NodeName)}";

                    page.AddText(line1, 12, new PdfPoint(50, currentY), fontBold);
                    page.AddText(line2, 10, new PdfPoint(50, currentY - 15), font);
                    page.AddText(line3, 10, new PdfPoint(50, currentY - 30), fontOblique);

                    currentY -= 55;
                    index++;

                    if (currentY < 100)
                    {
                        page = builder.AddPage(PageSize.A4);
                        currentY = 750;
                    }
                }
            }
            else
            {
                page.AddText("Excellent! You meet all the requirements for your Target Role.", 12, new PdfPoint(50, currentY), font);
                currentY -= 30;
            }

            page.AddText("Recommendation: Focus on HIGH priority skills. Access the Learning Hub links above to start.", 12, new PdfPoint(50, currentY - 20), fontBold);

            return builder.Build();
        }
    }
}