using Service_TechCompass.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service_TechCompass.Interfaces
{
    public interface ICounselorService
    {
        Task<List<StudentRoleStatDto>> GetStudentDistributionByRoleAsync();
        Task<List<CohortSkillGapDto>> GetTopCohortSkillGapsAsync(int topCount);

        // CÁC HÀM MỚI BỔ SUNG
        Task<PagedResult<CounselorStudentDto>> GetStudentsProgressAsync(int pageNumber, int pageSize, int? roleId);
        Task<AssessmentStatDto> GetAssessmentStatsAsync();
        Task<List<MarketAlignmentDto>> GetMarketAlignmentAsync();
    }
}