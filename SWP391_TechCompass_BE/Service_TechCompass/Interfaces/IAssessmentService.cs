using System.Collections.Generic;
using System.Threading.Tasks;
using Service_TechCompass.DTOs.Assessment;
using Repository_TechCompass.Models;

namespace Service_TechCompass.Interfaces
{
    public interface IAssessmentService
    {
        Task<List<QuizQuestionDto>> GetQuizBySkillNodeAsync(int skillNodeId);
        Task<SkillAssessment> GradeAndSaveQuizAsync(QuizSubmissionDto submission);

        Task<SkillAssessment> GradeAndSaveCodeTestAsync(CodeTestSubmissionDto submission);

        Task<AssessmentFeedbackDto> GetAssessmentFeedbackAsync(Guid assessmentId);

        Task<IEnumerable<object>> GetAllSkillNodesAsync();

        Task<CodingExercise> GetOrGenerateCodingExerciseAsync(int skillNodeId);

        Task<object?> GetMyLatestNodeResultAsync(Guid studentId, int skillNodeId);

        Task<IEnumerable<object>> GetMyAssessmentHistoryListAsync(Guid studentId);
        Task<object?> GetAssessmentDetailByIdAsync(Guid assessmentId);

        Task<AssessmentSession> GradeAndSaveFullExamAsync(SubmitFullExamDto submission);

        // Thêm 2 hàm này vào interface
        Task<List<QuizQuestionDto>> GetComprehensiveQuizByRoleAsync(int roleId);
        Task<CodingExercise> GetComprehensiveCodingExerciseByRoleAsync(int roleId);
    }
}