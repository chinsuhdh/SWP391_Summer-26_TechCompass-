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

        // 1. Lấy danh sách sinh viên
        public async Task<PagedResult<CounselorStudentDto>> GetStudentsProgressAsync(int pageNumber, int pageSize, int? roleId = null)
        {
            var query = _context.Students.Include(s => s.User).AsQueryable();

            int total = await query.CountAsync();

            var students = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new CounselorStudentDto
                {
                    StudentId = s.StudentId,
                    FullName = s.FullName,
                    Email = s.User != null ? s.User.Email : "",
                    StudentCode = s.StudentCode ?? "",
                    TargetRoleName = "Software Engineer", // Thay thế bằng DB thực tế
                    ProgressPercentage = new Random().Next(10, 100), // Dữ liệu giả lập
                    AiScore = new Random().Next(50, 95) // Dữ liệu giả lập
                })
                .ToListAsync();

            return new PagedResult<CounselorStudentDto>
            {
                Items = students,
                TotalCount = total,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        // 2. Thống kê bài kiểm tra
        public async Task<AssessmentStatDto> GetAssessmentStatsAsync()
        {
            // Tạm thời giả lập dữ liệu cho Frontend hiển thị
            return await Task.FromResult(new AssessmentStatDto
            {
                TotalSessions = 1250,
                PassRate = 85.5,
                AverageScore = 7.8
            });
        }

        // 3. Độ vênh thị trường
        public async Task<List<MarketAlignmentDto>> GetMarketAlignmentAsync()
        {
            // Tạm thời giả lập dữ liệu
            return await Task.FromResult(new List<MarketAlignmentDto>
            {
                new MarketAlignmentDto { SkillName = "C# / .NET", MarketDemandPercentage = 80, StudentAdoptionPercentage = 60 },
                new MarketAlignmentDto { SkillName = "ReactJS", MarketDemandPercentage = 90, StudentAdoptionPercentage = 85 },
                new MarketAlignmentDto { SkillName = "SQL Server", MarketDemandPercentage = 70, StudentAdoptionPercentage = 50 }
            });
        }

        // 4. Biểu đồ tròn
        public async Task<List<StudentRoleStatDto>> GetStudentDistributionByRoleAsync()
        {
            return await Task.FromResult(new List<StudentRoleStatDto>
            {
                new StudentRoleStatDto { RoleName = "Backend Dev", StudentCount = 40 },
                new StudentRoleStatDto { RoleName = "Frontend Dev", StudentCount = 30 },
                new StudentRoleStatDto { RoleName = "Data Analyst", StudentCount = 15 }
            });
        }

        // 5. Chi tiết Hồ sơ sinh viên
        public async Task<StudentPortfolioDto> GetStudentPortfolioAsync(Guid studentId)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null) return null!;

            return new StudentPortfolioDto
            {
                StudentName = student.FullName,
                AiCareerScore = 85,
                AiProfileSummary = "Sinh viên có nền tảng tư duy tốt, phù hợp phát triển Backend.",
                CareerRecommendation = new { recommendedRole = "Backend Developer", strengths = new[] { "Logic", "C#" } },
                SkillGapAnalysis = new { matchPercentage = 75, missingSkills = new[] { "Docker", "Redis" } },
                RoadmapProgress = new { progressPercentage = 60 },
                GithubStats = new { totalRepositories = 5, totalLanguages = 3 },
                Repositories = new List<object>()
            };
        }

        // 6. THỰC THI HÀM CÒN THIẾU: Cohort Analysis
        public async Task<List<CohortSkillGapDto>> GetTopCohortSkillGapsAsync(int topCount)
        {
            // Trả về dữ liệu Mock cho Controller
            var data = new List<CohortSkillGapDto>
            {
                new CohortSkillGapDto { SkillName = "Docker & Kubernetes", MissingCount = 120, ImpactLevel = "High" },
                new CohortSkillGapDto { SkillName = "System Design", MissingCount = 85, ImpactLevel = "High" },
                new CohortSkillGapDto { SkillName = "Cloud (AWS/Azure)", MissingCount = 70, ImpactLevel = "Medium" }
            };

            return await Task.FromResult(data.Take(topCount).ToList());
        }
    }
}