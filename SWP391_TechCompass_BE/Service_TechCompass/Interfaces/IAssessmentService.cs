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
    }
}