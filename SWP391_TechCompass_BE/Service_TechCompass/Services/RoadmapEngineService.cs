using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Repository_TechCompass;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;
// THÊM THƯ VIỆN SEMANTIC KERNEL ĐỂ GỌI AI
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Service_TechCompass.Services
{
    public class RoadmapEngineService : IRoadmapEngineService
    {
        private readonly IUserRepository _userRepo;
        private readonly Swp391CareerRoadmapContext _context;
        private readonly IChatCompletionService _chatCompletionService; // BỔ SUNG GEMINI

        public RoadmapEngineService(
            IUserRepository userRepo,
            Swp391CareerRoadmapContext context,
            Kernel kernel) // INJECT KERNEL
        {
            _userRepo = userRepo;
            _context = context;
            _chatCompletionService = kernel.GetRequiredService<IChatCompletionService>("GeminiChat");
        }

        public async Task<(int StatusCode, string Message, GenerateRoadmapResponseDto? Data)> GenerateRoadmapAsync(Guid userId)
        {
            var student = _userRepo.GetStudentByUserId(userId);
            if (student == null || student.TargetRoleId == null)
                return (400, "Sinh viên chưa chọn nghề nghiệp mục tiêu.", null);

            // 1. Tìm Lộ trình (TechPath) chuẩn cho Target Role này
            var techPath = await _context.TechPaths
                .FirstOrDefaultAsync(tp => tp.TargetRoleId == student.TargetRoleId);

            if (techPath == null) return (404, "Chưa có lộ trình chuẩn cho nghề nghiệp này.", null);

            // 2. Lấy tất cả kỹ năng (Nodes) của lộ trình này
            var nodes = await _context.SkillNodes
                .Where(n => n.TechPathId == techPath.TechPathId)
                .ToListAsync();

            int addedCount = 0;

            // 3. Khởi tạo tiến độ cho sinh viên vào bảng roadmap_progress
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
                        Status = "Not Started", // Trạng thái ban đầu
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

            // Kiểm tra xem Node Cha đã được sinh viên "Completed" chưa
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
            // Xóa bỏ hoặc Archive các Node chưa học, giữ nguyên Node đã học (Tùy logic)
            // Đơn giản nhất là chạy lại Generate để nạp bù các Node còn thiếu nếu TechPath có update
            return await GenerateRoadmapAsync(userId);
        }

        public async Task SyncProgressAfterAssessmentAsync(Guid userId, int skillNodeId, decimal totalQuizScore, decimal totalCodeScore)
        {
            // 1. Lấy tiến độ hiện tại của sinh viên với Node này
            var progress = await _context.RoadmapProgresses
                .FirstOrDefaultAsync(p => p.StudentId == userId && p.SkillNodeId == skillNodeId);

            // Nếu sinh viên nhảy cóc làm test mà chưa tạo Roadmap, tự động tạo mới Progress
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

            // 2. Logic Quyết định (Pass/Fail)
            // Giả sử điểm chuẩn để qua môn là: Quiz >= 5 VÀ Code >= 5
            bool isPassed = totalQuizScore >= 5.0m && totalCodeScore >= 5.0m;

            if (isPassed)
            {
                progress.Status = "Completed";
                progress.CompletionPercent = 100;
                progress.CompletedAt = DateTime.Now;
            }
            else
            {
                // Fail thì chuyển trạng thái sang Learning để yêu cầu học thêm tài liệu
                progress.Status = "Learning";

                // Tính % hoàn thành tượng trưng dựa trên tổng điểm (Max 20)
                decimal totalScore = totalQuizScore + totalCodeScore;
                progress.CompletionPercent = (int)((totalScore / 20.0m) * 100);
            }

            progress.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        // ==========================================
        // GIAI ĐOẠN 4: KHỞI TẠO ROADMAP TỪ BÀI TEST BẰNG AI
        // ==========================================
        public async Task<(int StatusCode, string Message, GenerateRoadmapResponseDto? Data)> GenerateAiRoadmapFromSessionAsync(Guid userId, Guid sessionId)
        {
            // 1. TÌM SESSION (Bài test sinh viên vừa nộp) & TRUY XUẤT NHÁNH NGÀNH LIÊN QUAN
            var session = await _context.AssessmentSessions
                .Include(s => s.SkillNode)
                    .ThenInclude(sn => sn.TechPath)
                        .ThenInclude(tp => tp.TargetRole)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null) return (404, "Không tìm thấy dữ liệu bài test.", null);

            var techPath = session.SkillNode.TechPath;
            var role = techPath.TargetRole;

            if (techPath == null || role == null) return (404, "Lỗi dữ liệu: Kỹ năng này chưa được map vào hệ thống lộ trình.", null);

            // 2. GÁN TARGET ROLE MỚI CHO SINH VIÊN (Dựa vào bài test vừa làm)
            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == userId || s.UserId == userId);
            if (student == null) return (404, "Không tìm thấy thông tin sinh viên.", null);

            student.TargetRoleId = role.TargetRoleId;

            // 3. ĐỔ TOÀN BỘ KHUNG ROADMAP VÀO BẢNG PROGRESS
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

            // 4. ĐỒNG BỘ ĐIỂM BÀI TEST VÀO NODE TƯƠNG ỨNG TRONG LỘ TRÌNH VỪA TẠO
            decimal totalScore = session.TotalQuizScore + session.TotalCodeScore;
            var currentProgress = await _context.RoadmapProgresses.FirstOrDefaultAsync(p => p.StudentId == student.StudentId && p.SkillNodeId == session.SkillNodeId);

            if (currentProgress != null)
            {
                // Logic chuẩn: Quiz >= 5 VÀ Code >= 5 thì MỚI PASS
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

            // 5. GỌI GEMINI AI ĐỂ PHÂN TÍCH VÀ ĐƯA RA LỜI KHUYÊN
            string aiAdvice = $"Đã kích hoạt Lộ trình {techPath.PathName}.";
            try
            {
                var chatHistory = new ChatHistory();
                chatHistory.AddSystemMessage("Bạn là chuyên gia Mentor IT, cố vấn lộ trình học tập theo chuẩn roadmap.sh. Hãy trả lời cực kỳ ngắn gọn dưới 80 chữ.");

                string prompt = $@"Sinh viên vừa kiểm tra kỹ năng '{session.SkillNode.NodeName}' đạt {totalScore}/20 điểm.
                 Lộ trình đích: '{techPath.PathName}' (Vị trí {role.RoleName}, nhu cầu thị trường đang rất hot: {role.MarketDemandIndex}/10).
                 Hãy viết 1 đoạn văn đóng vai AI Mentor: 
                 - Nếu điểm >= 10: Khen ngợi và giục họ mở khóa kỹ năng tiếp theo.
                 - Nếu điểm < 10: Khuyên họ xem lại tài liệu cơ bản.
                 Bắt buộc chèn keywords 'roadmap.sh' vào câu trả lời.";

                chatHistory.AddUserMessage(prompt);
                var aiResponse = await _chatCompletionService.GetChatMessageContentAsync(chatHistory);
                aiAdvice = aiResponse.ToString();
            }
            catch
            {
                // Fallback nếu API Google Gemini bị nghẽn
                aiAdvice = $"Dựa trên điểm số {totalScore}/20, hệ thống đã nạp chuẩn roadmap.sh và tạo thành công {addedCount} module kỹ năng cho vị trí {role.RoleName}. Hãy theo sát cây lộ trình để lấp đầy lỗ hổng nhé!";
            }

            var responseData = new GenerateRoadmapResponseDto
            {
                TargetRoleId = role.TargetRoleId,
                RoleName = techPath.PathName
            };

            return (200, aiAdvice, responseData);
        }
    }
}