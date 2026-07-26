using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Repository_TechCompass;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs;
using Service_TechCompass.Hubs;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class RoadmapService : IRoadmapService
    {
        private readonly IUserRepository _userRepo;
        private readonly Swp391CareerRoadmapContext _context;
        private readonly IRoadmapEngineService _engineService;
        private readonly IHubContext<RoadmapNotificationHub> _hubContext;

        public RoadmapService(
            IUserRepository userRepo,
            Swp391CareerRoadmapContext context,
            IRoadmapEngineService engineService,
            IHubContext<RoadmapNotificationHub> hubContext)
        {
            _userRepo = userRepo;
            _context = context;
            _engineService = engineService;
            _hubContext = hubContext;
        }

        public async Task<(int StatusCode, string Message, List<SkillNodeDto>? Data)> GetSkillTreeAsync(Guid userId)
        {
            var student = _userRepo.GetStudentByUserId(userId);
            if (student == null) return (404, "Không tìm thấy hồ sơ sinh viên.", null);

            if (student.TargetRoleId == null)
            {
                return (200, "Sinh viên chưa chọn ngành nghề mục tiêu.", new List<SkillNodeDto>());
            }

            // 1. CHỈ LẤY TECHPATH TƯƠNG ỨNG VỚI TARGET ROLE HIỆN TẠI CỦA SINH VIÊN
            var currentTechPath = await _context.TechPaths
                .FirstOrDefaultAsync(tp => tp.TargetRoleId == student.TargetRoleId);

            if (currentTechPath == null)
            {
                return (404, "Chưa cấu hình Lộ trình (TechPath) cho ngành nghề này.", null);
            }

            // 2. QUERY CHÍNH XÁC CÁC NODE THUỘC TECHPATH ĐÓ
            var query = from n in _context.SkillNodes
                        join p in _context.RoadmapProgresses
                            on new { n.SkillNodeId, StudentId = student.StudentId }
                            equals new { p.SkillNodeId, p.StudentId } into progressGroup
                        from p in progressGroup.DefaultIfEmpty() // Left Join để lấy cả những node chưa học
                        where n.TechPathId == currentTechPath.TechPathId
                        orderby n.PriorityLevel ascending
                        select new { n, p };

            var techPathNodes = await query.ToListAsync();

            // 3. QUERY "NHỊP ĐẬP THỊ TRƯỜNG" TRONG 30 NGÀY QUA
            var thirtyDaysAgo = DateOnly.FromDateTime(DateTime.Now.AddDays(-30));
            var recentTrends = await _context.TrendAnalyses
                .Where(t => t.AnalyzedDate >= thirtyDaysAgo)
                .GroupBy(t => t.SkillNodeId)
                .Select(g => new
                {
                    SkillNodeId = g.Key,
                    AverageTrendScore = g.Average(x => x.TrendScore)
                })
                .ToDictionaryAsync(k => k.SkillNodeId, v => v.AverageTrendScore);

            const decimal HOT_TREND_THRESHOLD = 2.5m;
            var skillTree = new List<SkillNodeDto>();

            // 4. MAP DỮ LIỆU
            foreach (var item in techPathNodes)
            {
                var validation = await _engineService.ValidatePrerequisiteAsync(student.StudentId, item.n.SkillNodeId);

                decimal currentScore = recentTrends.ContainsKey(item.n.SkillNodeId)
                                        ? (recentTrends[item.n.SkillNodeId] ?? 0)
                                        : 0;

                skillTree.Add(new SkillNodeDto
                {
                    NodeId = item.n.SkillNodeId,
                    NodeName = item.n.NodeName,
                    Description = item.n.Description,
                    ParentNodeId = item.n.ParentNodeId,
                    // Nếu chưa có record progress hoặc status != Completed thì IsCompleted = false
                    IsCompleted = item.p != null && item.p.Status == "Completed",
                    IsLocked = !validation.IsValid,
                    IsTrending = currentScore >= HOT_TREND_THRESHOLD,
                    CurrentTrendScore = Math.Round(currentScore, 2)
                });
            }

            return (200, "Lấy sơ đồ Roadmap thành công.", skillTree);
        }

        // =========================================================
        // [BUG-008 FIX]: BỔ SUNG ASSESSMENT GATE ĐỂ BẢO VỆ TIẾN ĐỘ
        // =========================================================
        public async Task<(int StatusCode, string Message)> MarkNodeCompletedAsync(Guid userId, MarkNodeCompletedDto request)
        {
            var student = _userRepo.GetStudentByUserId(userId);
            if (student == null)
            {
                return (404, "Không tìm thấy hồ sơ sinh viên.");
            }

            // 1. Kiểm tra điều kiện tiên quyết (Node cha đã mở khóa chưa?)
            var validation = await _engineService.ValidatePrerequisiteAsync(student.StudentId, request.NodeId);
            if (!validation.IsValid)
            {
                return (403, "Bạn không thể hoàn thành bài học đang bị khóa.");
            }

            // 2. Lấy thông tin Skill Node để kiểm tra loại bài test (Có bắt buộc thi Code không)
            var node = await _context.SkillNodes.FindAsync(request.NodeId);
            if (node == null)
            {
                return (404, "Kỹ năng không tồn tại trong hệ thống.");
            }

            // 3. ASSESSMENT GATE: Kiểm tra xem sinh viên đã thực sự thi đạt Node này chưa
            bool isCodingRequired = node.IsCodingRequired ?? false;

            var passedSession = await _context.AssessmentSessions
                .AnyAsync(s => s.StudentId == student.StudentId
                            && s.SkillNodeId == request.NodeId
                            && (s.AssessmentType == "TESTED" || s.AssessmentType == "SELF_DECLARED")
                            && (isCodingRequired
                                ? (s.TotalQuizScore >= 5.0m && s.TotalCodeScore >= 5.0m)
                                : s.TotalQuizScore >= 5.0m));

            // Nếu chưa đạt bài test và không có cờ ép buộc (ForceComplete từ Admin) -> Chặn lại
            if (!passedSession)
            {
                return (400, "Bạn chưa vượt qua bài kiểm tra năng lực cho kỹ năng này. Vui lòng hoàn thành bài test trước.");
            }

            // 4. Cập nhật hoặc Khởi tạo tiến độ trong Database
            var progress = await _context.RoadmapProgresses
                .FirstOrDefaultAsync(p => p.StudentId == student.StudentId && p.SkillNodeId == request.NodeId);

            if (progress == null)
            {
                progress = new RoadmapProgress
                {
                    ProgressId = Guid.NewGuid(),
                    StudentId = student.StudentId,
                    SkillNodeId = request.NodeId,
                    Status = "Completed",
                    CompletionPercent = 100,
                    CompletedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                _context.RoadmapProgresses.Add(progress);
            }
            else
            {
                progress.Status = "Completed";
                progress.CompletionPercent = 100;
                progress.CompletedAt = DateTime.Now;
                progress.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            // 5. Bắn tín hiệu Real-time qua SignalR
            await _hubContext.Clients.Group($"roadmap_user_{userId}").SendAsync("ReceiveRoadmapUpdate", new
            {
                NodeId = request.NodeId,
                Status = "Completed",
                Message = "Kỹ năng đã được cập nhật hoàn thành."
            });

            return (200, "Chúc mừng! Bạn đã hoàn thành kỹ năng.");
        }

        public async Task<(int StatusCode, string Message, DashboardSummaryDto? Data)> GetStudentDashboardAsync(Guid userId)
        {
            var student = _userRepo.GetStudentByUserId(userId);
            if (student == null)
            {
                return (404, "Không tìm thấy hồ sơ sinh viên.", null);
            }

            var progressList = await _context.RoadmapProgresses
                .Include(p => p.SkillNode)
                .Where(p => p.StudentId == student.StudentId)
                .ToListAsync();

            if (!progressList.Any()) return (404, "Chưa có lộ trình học tập. Vui lòng tạo lộ trình.", null);

            int totalNodes = progressList.Count;
            int completedNodes = progressList.Count(p => p.Status == "Completed");
            double progressPercent = totalNodes > 0 ? Math.Round(((double)completedNodes / totalNodes) * 100, 2) : 0;

            SkillNodeDto? nextSkillDto = null;
            foreach (var p in progressList.Where(x => x.Status != "Completed"))
            {
                var validation = await _engineService.ValidatePrerequisiteAsync(student.StudentId, p.SkillNodeId);
                if (validation.IsValid)
                {
                    nextSkillDto = new SkillNodeDto
                    {
                        NodeId = p.SkillNodeId,
                        NodeName = p.SkillNode.NodeName,
                        Description = p.SkillNode.Description
                    };
                    break;
                }
            }

            var dashboardData = new DashboardSummaryDto
            {
                TotalNodes = totalNodes,
                CompletedNodes = completedNodes,
                ProgressPercentage = progressPercent,
                NextSkill = nextSkillDto,
                TechTrends = new List<TechTrendDto>()
            };

            return (200, "Lấy thông tin tiến độ thành công.", dashboardData);
        }
    }
}