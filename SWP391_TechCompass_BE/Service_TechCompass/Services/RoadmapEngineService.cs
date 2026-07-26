using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Repository_TechCompass;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Service_TechCompass.Services
{
    public class RoadmapEngineService : IRoadmapEngineService
    {
        private readonly IUserRepository _userRepo;
        private readonly Swp391CareerRoadmapContext _context;
        private readonly IChatCompletionService _aiRoadmapAnalyzer;

        public RoadmapEngineService(
            IUserRepository userRepo,
            Swp391CareerRoadmapContext context,
            Kernel kernel)
        {
            _userRepo = userRepo;
            _context = context;
            _aiRoadmapAnalyzer = kernel.GetRequiredService<IChatCompletionService>("OpenAiCodeAnalyzer");
        }

        public async Task<(int StatusCode, string Message, GenerateRoadmapResponseDto? Data)> GenerateRoadmapAsync(Guid userId)
        {
            var student = _userRepo.GetStudentByUserId(userId);
            if (student == null || student.TargetRoleId == null)
                return (400, "Sinh viên chưa chọn nghề nghiệp mục tiêu.", null);

            var techPath = await _context.TechPaths
                .FirstOrDefaultAsync(tp => tp.TargetRoleId == student.TargetRoleId);

            if (techPath == null) return (404, "Chưa có lộ trình chuẩn cho nghề nghiệp này.", null);

            var nodes = await _context.SkillNodes
                .Where(n => n.TechPathId == techPath.TechPathId)
                .ToListAsync();

            int addedCount = 0;

            foreach (var node in nodes)
            {
                bool exists = await _context.RoadmapProgresses
                    .AnyAsync(p => p.StudentId == userId && p.SkillNodeId == node.SkillNodeId);

                if (!exists)
                {
                    var progress = new RoadmapProgress
                    {
                        ProgressId = Guid.NewGuid(),
                        StudentId = student.StudentId, 
                        SkillNodeId = node.SkillNodeId,
                        Status = "Not Started",
                        CompletionPercent = 0,
                        UpdatedAt = DateTime.Now
                    };

                    _context.RoadmapProgresses.Add(progress);
                    addedCount++;
                }
            }

            await _context.SaveChangesAsync();

            var response = new GenerateRoadmapResponseDto
            {
                TargetRoleId = student.TargetRoleId.Value,
                RoleName = techPath.PathName
            };

            return (200, $"Tạo lộ trình thành công. Đã thêm {addedCount} kỹ năng mới vào bản đồ của bạn.", response);
        }

        public async Task<(int StatusCode, string Message, bool IsValid)> ValidatePrerequisiteAsync(Guid userId, int nodeId)
        {
            var node = await _context.SkillNodes.FindAsync(nodeId);
            if (node == null) return (404, "Không tìm thấy Node.", false);

            if (node.ParentNodeId == null)
                return (200, "Node gốc, không bị khóa.", true);

            var parentProgress = await _context.RoadmapProgresses
                .FirstOrDefaultAsync(p => p.StudentId == userId && p.SkillNodeId == node.ParentNodeId);

            if (parentProgress != null && parentProgress.Status == "Completed")
            {
                return (200, "Đã đủ điều kiện mở khóa.", true);
            }

            return (403, "Node bị khóa. Hãy hoàn thành bài học trước đó.", false);
        }

        public async Task<(int StatusCode, string Message, GenerateRoadmapResponseDto? Data)> RecalculateRoadmapAsync(Guid userId)
        {
            return await GenerateRoadmapAsync(userId);
        }

        public async Task SyncProgressAfterAssessmentAsync(Guid userId, int skillNodeId, decimal totalQuizScore, decimal totalCodeScore)
        {
            var progress = await _context.RoadmapProgresses
                .FirstOrDefaultAsync(p => p.StudentId == userId && p.SkillNodeId == skillNodeId);

            if (progress == null)
            {
                progress = new RoadmapProgress
                {
                    ProgressId = Guid.NewGuid(),
                    StudentId = userId,
                    SkillNodeId = skillNodeId,
                    UpdatedAt = DateTime.Now
                };
                _context.RoadmapProgresses.Add(progress);
            }

            bool isPassed = totalQuizScore >= 5.0m && totalCodeScore >= 5.0m;

            if (isPassed)
            {
                progress.Status = "Completed";
                progress.CompletionPercent = 100;
                progress.CompletedAt = DateTime.Now;
            }
            else
            {
                progress.Status = "Learning";
                decimal totalScore = totalQuizScore + totalCodeScore;
                progress.CompletionPercent = (int)((totalScore / 20.0m) * 100);
            }

            progress.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        // =========================================================
        // TÁI CẤU TRÚC: CHỈ XỬ LÝ ĐỒNG BỘ TIẾN ĐỘ & PHÂN TÍCH AI MENTOR
        // =========================================================
        public async Task<(int StatusCode, string Message, object? Data)> ProcessAssessmentResultAsync(Guid userId, Guid sessionId)
        {
            var session = await _context.AssessmentSessions
                .Include(s => s.SkillNode)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null) return (404, "Không tìm thấy dữ liệu bài test.", null);

            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == userId || s.UserId == userId);
            if (student == null) return (404, "Không tìm thấy thông tin sinh viên.", null);

            // 1. Tự động đồng bộ điểm số bài test vào tiến độ Cây Roadmap hiện có của sinh viên
            decimal totalScore = session.TotalQuizScore + session.TotalCodeScore;
            await SyncProgressAfterAssessmentAsync(student.StudentId, session.SkillNodeId, session.TotalQuizScore, session.TotalCodeScore);

            // Nếu bài test có chi tiết từng câu hỏi placement, đồng bộ thêm danh sách node placement
            if (session.QuizDetails != null && session.QuizDetails.Any())
            {
                await SyncPlacementTestProgressAsync(student.StudentId, session.QuizDetails.ToList());
            }

            // 2. Gọi AI Mentor nhận xét súc tích
            string aiAdvice = string.Empty;
            try
            {
                var chatHistory = new ChatHistory();
                chatHistory.AddSystemMessage("Bạn là chuyên gia IT Mentor. Hãy đưa ra nhận xét súc tích dưới 80 chữ cho sinh viên dựa trên điểm bài test.");

                string prompt = $@"Sinh viên vừa làm bài test kỹ năng '{session.SkillNode.NodeName}' đạt {totalScore}/20 điểm.
Hãy nhận xét ngắn gọn và khuyên họ bước tiếp theo nên làm gì trên cây Roadmap học tập.";

                chatHistory.AddUserMessage(prompt);
                var aiResponse = await _aiRoadmapAnalyzer.GetChatMessageContentAsync(chatHistory);
                aiAdvice = aiResponse.ToString();
            }
            catch
            {
                aiAdvice = totalScore >= 10
                    ? $"Chúc mừng! Bạn đã đạt {totalScore}/20 điểm cho kỹ năng {session.SkillNode.NodeName}. Kỹ năng này đã được đánh dấu hoàn thành trên Roadmap!"
                    : $"Bạn đạt {totalScore}/20 điểm cho kỹ năng {session.SkillNode.NodeName}. Hãy ôn tập lại tài liệu và làm lại bài test để nâng cao điểm số.";
            }

            var resultData = new
            {
                nodeId = session.SkillNodeId,
                nodeName = session.SkillNode.NodeName,
                totalScore = totalScore,
                aiAdvice = aiAdvice
            };

            return (200, "Đã cập nhật tiến độ bài test vào Roadmap thành công.", resultData);
        }

        public async Task SyncPlacementTestProgressAsync(Guid userId, List<AssessmentQuizDetail> quizDetails)
        {
            if (quizDetails == null || !quizDetails.Any()) return;

            var questionIds = quizDetails.Select(q => q.QuestionId).ToList();
            var questions = await _context.AssessmentQuestions
                                           .Where(q => questionIds.Contains(q.QuestionId))
                                           .ToListAsync();

            var nodeResults = quizDetails
                .Join(questions,
                      detail => detail.QuestionId,
                      question => question.QuestionId,
                      (detail, question) => new { question.SkillNodeId, detail.IsCorrect })
                .GroupBy(x => x.SkillNodeId)
                .Select(g => new
                {
                    SkillNodeId = g.Key,
                    TotalQuestions = g.Count(),
                    CorrectAnswers = g.Count(x => x.IsCorrect),
                    PassPercentage = (decimal)g.Count(x => x.IsCorrect) / g.Count()
                }).ToList();

            foreach (var result in nodeResults)
            {
                if (result.PassPercentage >= 0.60m)
                {
                    var progress = await _context.RoadmapProgresses
                        .FirstOrDefaultAsync(p => p.StudentId == userId && p.SkillNodeId == result.SkillNodeId);

                    if (progress == null)
                    {
                        _context.RoadmapProgresses.Add(new RoadmapProgress
                        {
                            ProgressId = Guid.NewGuid(),
                            StudentId = userId,
                            SkillNodeId = result.SkillNodeId,
                            Status = "Completed",
                            CompletionPercent = 100,
                            CompletedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        });
                    }
                    else
                    {
                        progress.Status = "Completed";
                        progress.CompletionPercent = 100;
                        progress.CompletedAt = DateTime.Now;
                        progress.UpdatedAt = DateTime.Now;
                    }
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}