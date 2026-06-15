using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;

namespace Repository_TechCompass.Repositories
{
    public class AssessmentRepository : IAssessmentRepository
    {
        private readonly Swp391CareerRoadmapContext _context;

        public AssessmentRepository(Swp391CareerRoadmapContext context)
        {
            _context = context;
        }

        public async Task<SkillAssessment> SaveAssessmentResultAsync(SkillAssessment assessment)
        {
            _context.SkillAssessments.Add(assessment);
            await _context.SaveChangesAsync();
            return assessment; // Trả về entity đã có AssessmentId
        }

        // Thêm implement cho hàm vừa khai báo
        public async Task<bool> SaveQuestionsAsync(List<AssessmentQuestion> questions)
        {
            await _context.AssessmentQuestions.AddRangeAsync(questions);
            var saved = await _context.SaveChangesAsync();
            return saved > 0;
        }

        public async Task<List<AssessmentQuestion>> GetQuestionsBySkillNodeAsync(int skillNodeId, int limit = 10)
        {
            // Lấy random 10 câu hỏi thuộc kỹ năng này
            return await _context.AssessmentQuestions
                .Where(q => q.SkillNodeId == skillNodeId)
                .OrderBy(r => Guid.NewGuid()) // Trộn đề ngẫu nhiên
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<AssessmentQuestion>> GetQuestionsByIdsAsync(List<int> questionIds)
        {
            // Lấy ra đáp án chuẩn của các câu mà sinh viên vừa nộp
            return await _context.AssessmentQuestions
                .Where(q => questionIds.Contains(q.QuestionId))
                .ToListAsync();
        }

        public async Task<SkillAssessment?> GetAssessmentByIdAsync(Guid assessmentId)
        {
            return await _context.SkillAssessments
                .Include(a => a.SkillNode) // Lấy kèm thông tin Node kỹ năng
                .FirstOrDefaultAsync(a => a.AssessmentId == assessmentId);
        }

        // Thêm vào AssessmentRepository
        public async Task<List<SkillNode>> GetAllSkillNodesAsync()
        {
            // Kéo toàn bộ danh sách Node từ DB lên
            return await _context.SkillNodes.ToListAsync();
        }
    }
}