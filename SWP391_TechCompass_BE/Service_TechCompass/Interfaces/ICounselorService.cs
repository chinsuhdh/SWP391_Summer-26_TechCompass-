using Service_TechCompass.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service_TechCompass.Interfaces
{
    public interface ICounselorService
    {
        // Thống kê cho màn hình Dashboard
        Task<List<StudentRoleStatDto>> GetStudentDistributionByRoleAsync();
        Task<AssessmentStatDto> GetAssessmentStatsAsync();
        Task<List<MarketAlignmentDto>> GetMarketAlignmentAsync();

        // Quản lý sinh viên
        Task<PagedResult<CounselorStudentDto>> GetStudentsProgressAsync(int pageNumber, int pageSize, int? roleId = null);
        Task<StudentPortfolioDto> GetStudentPortfolioAsync(Guid studentId);

        // ĐÃ BỔ SUNG: Phân tích kỹ năng diện rộng (Dành cho Cohort Analysis)
        Task<List<CohortSkillGapDto>> GetTopCohortSkillGapsAsync(int topCount);
    }
}