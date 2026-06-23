using Microsoft.EntityFrameworkCore;
using Repository_TechCompass;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service_TechCompass.Services
{
    public class CounselorService : ICounselorService
    {
        private readonly Swp391CareerRoadmapContext _context;

        public CounselorService(Swp391CareerRoadmapContext context)
        {
            _context = context;
        }

        // 1. Thống kê số lượng sinh viên chọn từng vị trí công việc (Target Career Role)
        public async Task<List<StudentRoleStatDto>> GetStudentDistributionByRoleAsync()
        {
            var stats = await _context.Students
                .GroupBy(s => s.TargetRoleId)
                .Select(g => new StudentRoleStatDto
                {
                    TargetRoleId = g.Key ?? 0,
                    RoleName = _context.TargetCareerRoles
                                      .Where(r => r.TargetRoleId == g.Key)
                                      .Select(r => r.RoleName)
                                      .FirstOrDefault() ?? "Chưa chọn định hướng",
                    StudentCount = g.Count()
                })
                .OrderByDescending(x => x.StudentCount)
                .ToListAsync();

            return stats;
        }

        // 2. Phân tích lỗ hổng kiến thức toàn khóa
        public async Task<List<CohortSkillGapDto>> GetTopCohortSkillGapsAsync(int topCount)
        {
            double totalStudents = await _context.Students.CountAsync();
            if (totalStudents == 0) return new List<CohortSkillGapDto>();

            // FIX DỨT ĐIỂM: Thay thế !p.IsCompleted bằng p.Status != "Completed" theo đúng DB Context
            var topGaps = await _context.RoadmapProgresses
                .Where(p => p.Status != "Completed")
                .GroupBy(p => p.SkillNodeId)
                .Select(g => new
                {
                    SkillNodeId = g.Key,
                    MissingCount = g.Count()
                })
                .OrderByDescending(x => x.MissingCount)
                .Take(topCount)
                .ToListAsync();

            var result = new List<CohortSkillGapDto>();

            foreach (var item in topGaps)
            {
                var nodeName = await _context.SkillNodes
                    .Where(n => n.SkillNodeId == item.SkillNodeId)
                    .Select(n => n.NodeName)
                    .FirstOrDefaultAsync() ?? "Kỹ năng ẩn";

                result.Add(new CohortSkillGapDto
                {
                    SkillNodeId = item.SkillNodeId,
                    SkillNodeName = nodeName,
                    MissingStudentCount = item.MissingCount,
                    DeficiencyPercentage = Math.Round((item.MissingCount / totalStudents) * 100, 2)
                });
            }

            return result;
        }
    }
}