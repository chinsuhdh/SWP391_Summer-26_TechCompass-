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
                return new List<SkillGapItemDto>();
            }

            // 1. LẤY TÊN TARGET ROLE (Ngữ cảnh Mục tiêu)
            var role = await _context.Roles.FindAsync(student.TargetRoleId);
            string targetRoleName = role != null ? role.RoleName : $"Role ID: {student.TargetRoleId}";

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

            var result = new List<SkillGapItemDto>();

            foreach (var node in requiredNodes)
            {
                var session = userSessions.FirstOrDefault(s => s.SkillNodeId == node.SkillNodeId);
                decimal currentPercent = session != null ? (session.MaxScore / 20.0m) * 100m : 0m;

                result.Add(new SkillGapItemDto
                {
                    NodeName = node.NodeName,
                    CurrentScore = Math.Round(currentPercent, 0),
                    TargetScore = 80,
                    RoleName = targetRoleName // Truyền tên Role ra ngoài
                });
            }

            return result;
        }

        public async Task<byte[]> GeneratePdfReportAsync(object reportData)
        {
            var data = reportData as List<SkillGapItemDto>;
            if (data == null || !data.Any()) throw new ArgumentException("Dữ liệu report không hợp lệ.");

            // Lấy tên Role từ phần tử đầu tiên
            string targetRoleName = data.First().RoleName;

            PdfDocumentBuilder builder = new PdfDocumentBuilder();
            PdfPageBuilder page = builder.AddPage(PageSize.A4);

            var font = builder.AddStandard14Font(Standard14Font.Helvetica);
            var fontBold = builder.AddStandard14Font(Standard14Font.HelveticaBold);
            var fontOblique = builder.AddStandard14Font(Standard14Font.HelveticaOblique); // Dùng cho Link

            // Vẽ Header & Ngữ cảnh Mục tiêu
            page.AddText("TECH COMPASS - SKILL GAP ANALYSIS REPORT", 16, new PdfPoint(50, 750), fontBold);
            page.AddText($"Generated Date: {DateTime.Now:yyyy-MM-dd HH:mm}", 10, new PdfPoint(50, 730), font);

            // ĐÃ THÊM: Ngữ cảnh Target Role
            page.AddText($"Target Role: {targetRoleName}", 12, new PdfPoint(50, 715), fontBold);
            page.AddText("--------------------------------------------------------------------------------", 12, new PdfPoint(50, 700), font);

            page.AddText("Missing & Improvement Areas:", 14, new PdfPoint(50, 670), fontBold);

            int currentY = 640;

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

                    // ĐÃ SỬA: Cụ thể hóa bằng hệ quy chiếu và thêm Link
                    string line1 = $"{index}. {skill.NodeName} - Priority: {priority}";
                    string line2 = $"    Current Skill Level: {skill.CurrentScore}% | Market Target: {skill.TargetScore}% -> Gap: {gapSize}%";
                    string line3 = $"    Learning Link: http://localhost:5173/dashboard/learning?skill={Uri.EscapeDataString(skill.NodeName)}";

                    page.AddText(line1, 12, new PdfPoint(50, currentY), fontBold);
                    page.AddText(line2, 10, new PdfPoint(50, currentY - 15), font);
                    page.AddText(line3, 10, new PdfPoint(50, currentY - 30), fontOblique); // In nghiêng cho giống link

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

            // Lời khuyên cuối cùng (Action Call)
            page.AddText("Recommendation: Focus on HIGH priority skills. Access the Learning Hub links above to start.", 12, new PdfPoint(50, currentY - 20), fontBold);

            return builder.Build();
        }
    }
}