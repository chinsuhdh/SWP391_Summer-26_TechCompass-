using Service_TechCompass.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service_TechCompass.Interfaces
{
    public interface ICounselorService
    {
        Task<List<StudentRoleStatDto>> GetStudentDistributionByRoleAsync();
        Task<List<CohortSkillGapDto>> GetTopCohortSkillGapsAsync(int topCount);
    }
}