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

        // 1. Thống kê số lượng sinh viên chọn từng vị trí công việc
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

        // 3. Lấy danh sách tiến độ sinh viên (Phân trang)
        public async Task<PagedResult<CounselorStudentDto>> GetStudentsProgressAsync(int pageNumber, int pageSize, int? roleId)
        {
            var query = _context.Students
                .Include(s => s.User)
                .Include(s => s.RoadmapProgresses)
                .Include(s => s.TargetRole) // <-- BƯỚC 1: INCLUDE TRỰC TIẾP TARGET ROLE VÀO ĐÂY
                .AsQueryable();

            if (roleId.HasValue)
            {
                query = query.Where(s => s.TargetRoleId == roleId.Value);
            }

            var totalRecords = await query.CountAsync();

            var studentsData = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var studentDtos = studentsData.Select(s => new CounselorStudentDto
            {
                StudentId = s.StudentId,
                FullName = s.FullName ?? "Unknown",

                TargetRoleName = s.TargetRole?.RoleName ?? "Chưa rõ",

                ProgressPercentage = s.RoadmapProgresses.Any()
                    ? Math.Round((double)s.RoadmapProgresses.Count(p => p.Status == "Completed") / s.RoadmapProgresses.Count * 100, 2)
                    : 0
            }).ToList();

            return new PagedResult<CounselorStudentDto>
            {
                Items = studentDtos,
                TotalCount = totalRecords,
                PageSize = pageSize,
                PageNumber = pageNumber
            };
        }

        // 4. Thống kê hiệu suất làm bài Assessment
        public async Task<AssessmentStatDto> GetAssessmentStatsAsync()
        {
            var sessions = await _context.AssessmentSessions.ToListAsync();

            if (!sessions.Any())
            {
                return new AssessmentStatDto { TotalSessions = 0, AverageScore = 0, PassRate = 0 };
            }

            var total = sessions.Count;
            // SỬA LỖI: Dùng TotalQuizScore (hoặc tổng của TotalQuizScore và TotalCodeScore tùy nghiệp vụ của bạn)
            var avgScore = sessions.Average(s => s.TotalQuizScore);
            var passCount = sessions.Count(s => s.TotalQuizScore >= 5m);

            return new AssessmentStatDto
            {
                TotalSessions = total,
                AverageScore = Math.Round((double)avgScore, 2),
                PassRate = Math.Round((double)passCount / total * 100, 2)
            };
        }

        // 5. Độ vênh giữa Market (FR4) và Sinh viên (Market Alignment)
        public async Task<List<MarketAlignmentDto>> GetMarketAlignmentAsync()
        {
            double totalStudents = await _context.Students.CountAsync();

            // SỬA LỖI: TrendAnalysis có khóa ngoại SkillNodeId, ta có thể Include trực tiếp
            var topMarketTrends = await _context.TrendAnalyses
                .Include(t => t.SkillNode)
                .OrderByDescending(t => t.TrendScore)
                .Take(5)
                .ToListAsync();

            var alignmentList = new List<MarketAlignmentDto>();

            foreach (var trend in topMarketTrends)
            {
                // Truy vấn thẳng bằng SkillNodeId, không cần dùng String Contains nữa
                int studentLearningCount = 0;

                if (trend.SkillNodeId != null)
                {
                    studentLearningCount = await _context.RoadmapProgresses
                        .Where(p => p.SkillNodeId == trend.SkillNodeId)
                        .Select(p => p.StudentId)
                        .Distinct()
                        .CountAsync();
                }

                alignmentList.Add(new MarketAlignmentDto
                {
                    SkillName = trend.SkillNode?.NodeName ?? "Unknown Skill",
                    MarketDemandPercentage = Math.Round((double)(trend.TrendScore ?? 0) * 100, 2),
                    StudentAdoptionPercentage = totalStudents == 0 ? 0 : Math.Round((studentLearningCount / totalStudents) * 100, 2)
                });
            }

            return alignmentList;
        }
    }
}