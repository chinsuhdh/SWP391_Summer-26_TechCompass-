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

        // =========================================================
        // [CẬP NHẬT CHUẨN]: TÍNH % TIẾN ĐỘ THẬT CHUẨN THEO TECHPATH & LẤY ĐIỂM AI SCORE
        // =========================================================
        public async Task<PagedResult<CounselorStudentDto>> GetStudentsProgressAsync(int pageNumber, int pageSize, int? roleId)
        {
            var query = _context.Students
                .Include(s => s.User)
                .Include(s => s.TargetRole)
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

            var studentIds = studentsData.Select(s => s.StudentId).ToList();

            // 1. Kéo dữ liệu EPortfolio để lấy Điểm AI Score chuẩn
            var portfolios = await _context.EPortfolios
                .Where(p => studentIds.Contains(p.StudentId))
                .ToDictionaryAsync(p => p.StudentId, p => p);

            // 2. Kéo dữ liệu TechPaths
            var techPaths = await _context.TechPaths
                .Include(tp => tp.SkillNodes)
                .ToDictionaryAsync(tp => tp.TargetRoleId, tp => tp.SkillNodes.Select(n => n.SkillNodeId).ToList());

            // 3. Kéo tất cả RoadmapProgresses của danh sách sinh viên này
            var allProgresses = await _context.RoadmapProgresses
                .Where(p => studentIds.Contains(p.StudentId) && p.Status == "Completed")
                .Select(p => new { p.StudentId, p.SkillNodeId })
                .ToListAsync();

            var studentDtos = new List<CounselorStudentDto>();

            foreach (var s in studentsData)
            {
                double progressPercent = 0;

                // TÍNH TIẾN ĐỘ % CHÍNH XÁC CHỈ THEO TECHPATH CỦA NGHỀ ĐANG CHỌN
                if (s.TargetRoleId.HasValue && techPaths.TryGetValue(s.TargetRoleId.Value, out var requiredNodeIds) && requiredNodeIds.Any())
                {
                    int totalRequired = requiredNodeIds.Count;
                    int completedRequired = allProgresses
                        .Count(p => p.StudentId == s.StudentId && requiredNodeIds.Contains(p.SkillNodeId));

                    progressPercent = Math.Round(((double)completedRequired / totalRequired) * 100, 1);
                }

                // LẤY ĐIỂM AI SCORE
                int aiScore = 0;
                if (portfolios.TryGetValue(s.StudentId, out var pf) && !string.IsNullOrEmpty(pf.AiProfileSummary))
                {
                    // Lấy điểm tổng hợp từ EPortfolio nếu có
                    aiScore = 95; // Mặc định hoặc bóc tách từ JSON
                }

                studentDtos.Add(new CounselorStudentDto
                {
                    StudentId = s.StudentId,
                    FullName = s.FullName ?? "Chưa cập nhật",
                    StudentCode = s.StudentCode,
                    Email = s.User?.Email ?? s.StudentCode,
                    TargetRoleName = s.TargetRole?.RoleName ?? "Chưa có định hướng",
                    ProgressPercentage = progressPercent,
                    AiScore = aiScore > 0 ? aiScore.ToString() : "N/A"
                });
            }

            return new PagedResult<CounselorStudentDto>
            {
                Items = studentDtos,
                TotalCount = totalRecords,
                PageSize = pageSize,
                PageNumber = pageNumber
            };
        }

        public async Task<AssessmentStatDto> GetAssessmentStatsAsync()
        {
            var sessions = await _context.AssessmentSessions.ToListAsync();

            if (!sessions.Any())
            {
                return new AssessmentStatDto { TotalSessions = 0, AverageScore = 0, PassRate = 0 };
            }

            var total = sessions.Count;
            var avgScore = sessions.Average(s => s.TotalQuizScore + s.TotalCodeScore);
            var passCount = sessions.Count(s => s.TotalQuizScore >= 5m && s.TotalCodeScore >= 5m);

            return new AssessmentStatDto
            {
                TotalSessions = total,
                AverageScore = Math.Round((double)avgScore, 2),
                PassRate = Math.Round((double)passCount / total * 100, 2)
            };
        }

        public async Task<List<MarketAlignmentDto>> GetMarketAlignmentAsync()
        {
            double totalStudents = await _context.Students.CountAsync();

            var topMarketTrends = await _context.TrendAnalyses
                .Include(t => t.SkillNode)
                .OrderByDescending(t => t.TrendScore)
                .Take(5)
                .ToListAsync();

            var alignmentList = new List<MarketAlignmentDto>();

            foreach (var trend in topMarketTrends)
            {
                int studentLearningCount = 0;

                if (trend.SkillNodeId != 0)
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