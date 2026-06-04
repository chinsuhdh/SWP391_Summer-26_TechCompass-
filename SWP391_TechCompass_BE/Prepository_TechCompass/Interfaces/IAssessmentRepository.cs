using System.Threading.Tasks;
using Repository_TechCompass.Models;

namespace Repository_TechCompass.Interfaces
{
    public interface IAssessmentRepository
    {
        Task<SkillAssessment> SaveAssessmentResultAsync(SkillAssessment assessment);
        Task<bool> SaveQuestionsAsync(List<AssessmentQuestion> questions);

        Task<List<AssessmentQuestion>> GetQuestionsBySkillNodeAsync(int skillNodeId, int limit = 10);
        Task<List<AssessmentQuestion>> GetQuestionsByIdsAsync(List<int> questionIds);

        Task<SkillAssessment?> GetAssessmentByIdAsync(Guid assessmentId);
    }
}