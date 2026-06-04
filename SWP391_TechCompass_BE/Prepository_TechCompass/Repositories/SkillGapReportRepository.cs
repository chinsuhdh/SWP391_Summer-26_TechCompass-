using Microsoft.EntityFrameworkCore;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;

namespace Repository_TechCompass.Repositories
{
    public class SkillGapReportRepository : ISkillGapReportRepository
    {
        private readonly Swp391CareerRoadmapContext _context;

        public SkillGapReportRepository(Swp391CareerRoadmapContext context)
        {
            _context = context;
        }

        public async Task<Student?> GetStudentWithSkillsAndTargetAsync(Guid studentId)
        {
            return await _context.Students
                .Include(s => s.TargetRole)
                    .ThenInclude(tr => tr.TechPaths)
                        .ThenInclude(tp => tp.SkillNodes)
                .Include(s => s.SkillAssessments)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);
        }

        public async Task<SkillGapReport> SaveReportAsync(SkillGapReport report)
        {
            _context.SkillGapReports.Add(report);
            await _context.SaveChangesAsync();
            return report;
        }
    }
}