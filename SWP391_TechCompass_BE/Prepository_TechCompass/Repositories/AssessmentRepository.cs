using System;
using System.Collections.Generic;
using System.Linq;
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

        // ==========================================
        // CÁC HÀM XỬ LÝ ĐỀ THI / CÂU HỎI (QUIZ & EXERCISE)
        // ==========================================

        public async Task<bool> SaveQuestionsAsync(List<AssessmentQuestion> questions)
        {
            await _context.AssessmentQuestions.AddRangeAsync(questions);
            var saved = await _context.SaveChangesAsync();
            return saved > 0;
        }

        public async Task<List<AssessmentQuestion>> GetQuestionsBySkillNodeAsync(int skillNodeId, int limit = 10)
        {
            return await _context.AssessmentQuestions
                .Where(q => q.SkillNodeId == skillNodeId)
                .OrderBy(r => Guid.NewGuid()) // Trộn đề ngẫu nhiên
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<AssessmentQuestion>> GetQuestionsByIdsAsync(List<int> questionIds)
        {
            return await _context.AssessmentQuestions
                .Where(q => questionIds.Contains(q.QuestionId))
                .ToListAsync();
        }

        public async Task<List<SkillNode>> GetAllSkillNodesAsync()
        {
            return await _context.SkillNodes.ToListAsync();
        }

        public async Task<SkillNode?> GetSkillNodeByIdAsync(int skillNodeId)
        {
            return await _context.SkillNodes.FindAsync(skillNodeId);
        }

        public async Task<CodingExercise> SaveCodingExerciseAsync(CodingExercise exercise)
        {
            _context.CodingExercises.Add(exercise);
            await _context.SaveChangesAsync();
            return exercise;
        }

        public async Task<CodingExercise?> GetCodingExerciseByNodeAsync(int skillNodeId)
        {
            return await _context.CodingExercises.FirstOrDefaultAsync(c => c.SkillNodeId == skillNodeId);
        }


        // ==========================================
        // CÁC HÀM XỬ LÝ NEW ARCHITECTURE (ASSESSMENT SESSION)
        // ==========================================

        public async Task<AssessmentSession> SaveAssessmentSessionAsync(AssessmentSession session)
        {
            _context.AssessmentSessions.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }

        public async Task<AssessmentSession?> GetAssessmentSessionByIdAsync(Guid sessionId)
        {
            return await _context.AssessmentSessions
                .Include(s => s.SkillNode)
                .Include(s => s.CodeDetail) // Eager loading bảng con chứa AI Feedback
                .Include(s => s.QuizDetails) // Eager loading bảng con chứa đáp án trắc nghiệm
                    .ThenInclude(qd => qd.Question) // Kết nối sang bảng câu hỏi gốc để lấy text câu hỏi và đáp án đúng
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);
        }

        public async Task<AssessmentSession?> GetLatestAssessmentSessionByNodeAsync(Guid studentId, int skillNodeId)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == studentId || s.StudentId == studentId);

            Guid actualStudentId = student != null ? student.StudentId : studentId;

            return await _context.AssessmentSessions
                .Include(s => s.SkillNode)
                .Include(s => s.CodeDetail)
                .Where(s => s.StudentId == actualStudentId && s.SkillNodeId == skillNodeId)
                .OrderByDescending(s => s.TakenAt) // Lấy phiên làm gần nhất
                .FirstOrDefaultAsync();
        }

        public async Task<List<AssessmentSession>> GetAssessmentSessionsByStudentAsync(Guid studentId)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == studentId || s.StudentId == studentId);

            Guid actualStudentId = student != null ? student.StudentId : studentId;

            return await _context.AssessmentSessions
                .Include(s => s.SkillNode)
                .Where(s => s.StudentId == actualStudentId)
                .OrderByDescending(s => s.TakenAt) // Sắp xếp phiên mới nhất lên đầu
                .ToListAsync();
        }


        // ==========================================
        // CÁC HÀM LEGACY (BẢNG SKILL_ASSESSMENT CŨ - GIỮ LẠI TRÁNH LỖI DỰ ÁN)
        // ==========================================

        public async Task<SkillAssessment> SaveAssessmentResultAsync(SkillAssessment assessment)
        {
            _context.SkillAssessments.Add(assessment);
            await _context.SaveChangesAsync();
            return assessment;
        }

        public async Task<SkillAssessment?> GetAssessmentByIdAsync(Guid assessmentId)
        {
            return await _context.SkillAssessments
                .Include(a => a.SkillNode)
                .FirstOrDefaultAsync(a => a.AssessmentId == assessmentId);
        }

        public async Task<SkillAssessment?> GetLatestAssessmentByNodeAsync(Guid studentId, int skillNodeId)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == studentId || s.StudentId == studentId);
            Guid actualStudentId = student != null ? student.StudentId : studentId;

            return await _context.SkillAssessments
                .Include(a => a.SkillNode)
                .Where(a => a.SkillNodeId == skillNodeId)
                .OrderByDescending(a => a.TakenAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<SkillAssessment>> GetAssessmentsByStudentAsync(Guid studentId)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == studentId || s.StudentId == studentId);
            Guid actualStudentId = student != null ? student.StudentId : studentId;

            return await _context.SkillAssessments
                .Include(a => a.SkillNode)
                .OrderByDescending(a => a.TakenAt)
                .ToListAsync();
        }

        // Thêm hàm này vào trong class AssessmentRepository
        public async Task<List<SkillNode>> GetSkillNodesByRoleIdAsync(int roleId)
        {
            var techPath = await _context.TechPaths.FirstOrDefaultAsync(tp => tp.TargetRoleId == roleId);
            if (techPath == null) return new List<SkillNode>();

            return await _context.SkillNodes
                .Where(sn => sn.TechPathId == techPath.TechPathId)
                .OrderBy(sn => sn.PriorityLevel)
                .ToListAsync();
        }
    }
}