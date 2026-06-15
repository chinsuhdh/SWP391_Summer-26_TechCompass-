using System.Collections.Generic;
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

        Task<List<SkillNode>> GetAllSkillNodesAsync();

        // Lấy Node để biết tên kỹ năng (VD: "C#", "JavaScript")
        Task<SkillNode?> GetSkillNodeByIdAsync(int skillNodeId);

        // Lưu bài tập mới vào DB
        Task<CodingExercise> SaveCodingExerciseAsync(CodingExercise exercise);

        // Kiểm tra xem kỹ năng này đã có bài tập chưa (để không bắt AI sinh lại)
        Task<CodingExercise?> GetCodingExerciseByNodeAsync(int skillNodeId);
        Task<SkillAssessment?> GetLatestAssessmentByNodeAsync(Guid studentId, int skillNodeId);

        Task<List<SkillAssessment>> GetAssessmentsByStudentAsync(Guid studentId);

    }
}