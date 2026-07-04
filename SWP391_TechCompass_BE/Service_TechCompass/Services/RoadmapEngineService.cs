using System;
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
                        StudentId = userId,
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
        // CẬP NHẬT: LOGIC KIỂM TRA TRÁI NGÀNH VÀ YÊU CẦU XÁC NHẬN
        // =========================================================
        public async Task<(int StatusCode, string Message, object? Data)> GenerateAiRoadmapFromSessionAsync(Guid userId, Guid sessionId, bool confirmSwitch = false)
        {
            var session = await _context.AssessmentSessions
                .Include(s => s.SkillNode)
                    .ThenInclude(sn => sn.TechPath)
                        .ThenInclude(tp => tp.TargetRole)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null) return (404, "Không tìm thấy dữ liệu bài test.", null);

            var techPath = session.SkillNode.TechPath;
            var role = techPath.TargetRole;

            if (techPath == null || role == null) return (404, "Lỗi dữ liệu: Kỹ năng này chưa được map vào hệ thống lộ trình.", null);

            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == userId || s.UserId == userId);
            if (student == null) return (404, "Không tìm thấy thông tin sinh viên.", null);

            // 1. KIỂM TRA TRÁI NGÀNH
            if (student.TargetRoleId != null && student.TargetRoleId != role.TargetRoleId && !confirmSwitch)
            {
                return (202, $"Bài test này thuộc về lộ trình '{techPath.PathName}'. Bạn có muốn Cố vấn AI đổi định hướng nghề nghiệp của bạn sang ngành này không?", new { requiresConfirmation = true });
            }

            // 2. NẾU TRÙNG NGÀNH HOẶC ĐÃ ĐỒNG Ý ĐỔI NGÀNH -> CHẠY TIẾP LOGIC
            student.TargetRoleId = role.TargetRoleId;

            var nodes = await _context.SkillNodes.Where(n => n.TechPathId == techPath.TechPathId).ToListAsync();
            int addedCount = 0;
            foreach (var node in nodes)
            {
                bool exists = await _context.RoadmapProgresses.AnyAsync(p => p.StudentId == student.StudentId && p.SkillNodeId == node.SkillNodeId);
                if (!exists)
                {
                    _context.RoadmapProgresses.Add(new RoadmapProgress
                    {
                        ProgressId = Guid.NewGuid(),
                        StudentId = student.StudentId,
                        SkillNodeId = node.SkillNodeId,
                        Status = "Not Started",
                        CompletionPercent = 0,
                        UpdatedAt = DateTime.Now
                    });
                    addedCount++;
                }
            }
            await _context.SaveChangesAsync();

            decimal totalScore = session.TotalQuizScore + session.TotalCodeScore;
            var currentProgress = await _context.RoadmapProgresses.FirstOrDefaultAsync(p => p.StudentId == student.StudentId && p.SkillNodeId == session.SkillNodeId);

            if (currentProgress != null)
            {
                if (session.TotalQuizScore >= 5.0m && session.TotalCodeScore >= 5.0m)
                {
                    currentProgress.Status = "Completed";
                    currentProgress.CompletionPercent = 100;
                    currentProgress.CompletedAt = DateTime.Now;
                }
                else
                {
                    currentProgress.Status = "Learning";
                    currentProgress.CompletionPercent = (int)((totalScore / 20.0m) * 100);
                }
                currentProgress.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            string aiAdvice = $"Đã kích hoạt Lộ trình {techPath.PathName}.";
            try
            {
                var chatHistory = new ChatHistory();
                chatHistory.AddSystemMessage("Bạn là chuyên gia Mentor IT, cố vấn lộ trình học tập theo chuẩn roadmap.sh và đánh giá xu hướng thị trường. Hãy trả lời cực kỳ ngắn gọn dưới 80 chữ.");

                string prompt = $@"Sinh viên vừa kiểm tra kỹ năng '{session.SkillNode.NodeName}' đạt {totalScore}/20 điểm.
                 Lộ trình đích: '{techPath.PathName}' (Vị trí {role.RoleName}, nhu cầu thị trường đang rất hot: {role.MarketDemandIndex}/10).
                 Hãy viết 1 đoạn văn đóng vai AI Mentor: 
                 - Nếu điểm >= 10: Khen ngợi và giục họ mở khóa kỹ năng tiếp theo.
                 - Nếu điểm < 10: Khuyên họ xem lại tài liệu cơ bản.
                 Bắt buộc chèn keywords 'roadmap.sh' vào câu trả lời.";

                chatHistory.AddUserMessage(prompt);

                var aiResponse = await _aiRoadmapAnalyzer.GetChatMessageContentAsync(chatHistory);
                aiAdvice = aiResponse.ToString();
            }
            catch
            {
                aiAdvice = $"Dựa trên điểm số {totalScore}/20, hệ thống đã nạp chuẩn roadmap.sh và tạo thành công {addedCount} module kỹ năng cho vị trí {role.RoleName}. Hãy theo sát cây lộ trình để lấp đầy lỗ hổng nhé!";
            }

            var responseData = new GenerateRoadmapResponseDto
            {
                TargetRoleId = role.TargetRoleId,
                RoleName = techPath.PathName
            };

            return (200, aiAdvice, responseData);
        }

        // =========================================================
        // HÀM MỚI: ĐỒNG BỘ TIẾN ĐỘ TỪ BÀI TEST ĐẦU VÀO (PLACEMENT TEST)
        // =========================================================
        public async Task SyncPlacementTestProgressAsync(Guid userId, List<AssessmentQuizDetail> quizDetails)
        {
            if (quizDetails == null || !quizDetails.Any()) return;

            // 1. Lấy thông tin các câu hỏi để biết nó thuộc Skill Node nào
            var questionIds = quizDetails.Select(q => q.QuestionId).ToList();
            var questions = await _context.AssessmentQuestions
                                          .Where(q => questionIds.Contains(q.QuestionId))
                                          .ToListAsync();

            // 2. Gom nhóm kết quả theo từng Skill Node bằng LINQ Join
            // Ví dụ: Node C# (Đúng 2/3 câu), Node SQL (Đúng 3/3 câu), Node OOP (Đúng 0/3 câu)
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

            // 3. Duyệt qua từng Node, nếu Pass Rate >= 60% thì mở khóa và "tốt nghiệp" Node đó luôn!
            foreach (var result in nodeResults)
            {
                if (result.PassPercentage >= 0.60m) // Ngưỡng 60% (có thể tùy chỉnh)
                {
                    var progress = await _context.RoadmapProgresses
                        .FirstOrDefaultAsync(p => p.StudentId == userId && p.SkillNodeId == result.SkillNodeId);

                    // Nếu chưa có record trong bảng RoadmapProgress thì tạo mới với trạng thái Completed
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
                    // Nếu đã có (đang học dở) thì update lên Completed
                    else
                    {
                        progress.Status = "Completed";
                        progress.CompletionPercent = 100;
                        progress.CompletedAt = DateTime.Now;
                        progress.UpdatedAt = DateTime.Now;
                    }
                }
            }

            // Lưu toàn bộ thay đổi xuống Database 1 lần duy nhất để tối ưu hiệu năng
            await _context.SaveChangesAsync();
        }
    }
}