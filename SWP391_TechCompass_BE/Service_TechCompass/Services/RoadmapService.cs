using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Repository_TechCompass;
using Repository_TechCompass.Interfaces;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class RoadmapService : IRoadmapService
    {
        private readonly IUserRepository _userRepo;
        private readonly Swp391CareerRoadmapContext _context;
        private readonly IRoadmapEngineService _engineService; // Inject engine để check Khóa/Mở

        public RoadmapService(IUserRepository userRepo, Swp391CareerRoadmapContext context, IRoadmapEngineService engineService)
        {
            _userRepo = userRepo;
            _context = context;
            _engineService = engineService;
        }

        public async Task<(int StatusCode, string Message, List<SkillNodeDto>? Data)> GetSkillTreeAsync(Guid userId)
        {
            // 1. Join bảng SkillNodes và RoadmapProgress
            var query = from p in _context.RoadmapProgresses
                        join n in _context.SkillNodes on p.SkillNodeId equals n.SkillNodeId
                        where p.StudentId == userId
                        select new { p, n };

            var studentNodes = await query.ToListAsync();
            var skillTree = new List<SkillNodeDto>();

            // 2. Chuyển đổi và kiểm tra điều kiện Khóa (IsLocked)
            foreach (var item in studentNodes)
            {
                // Gọi sang Engine để xem Node này có bị khóa (do chưa học Node cha) không
                var validation = await _engineService.ValidatePrerequisiteAsync(userId, item.n.SkillNodeId);

                skillTree.Add(new SkillNodeDto
                {
                    NodeId = item.n.SkillNodeId,
                    NodeName = item.n.NodeName,
                    Description = item.n.Description,
                    ParentNodeId = item.n.ParentNodeId,
                    IsCompleted = (item.p.Status == "Completed"),
                    IsLocked = !validation.IsValid // Nếu Validate trả false -> Bị khóa
                });
            }

            return (200, "Lấy sơ đồ Roadmap thành công.", skillTree);
        }

        public async Task<(int StatusCode, string Message)> MarkNodeCompletedAsync(Guid userId, MarkNodeCompletedDto request)
        {
            var progress = await _context.RoadmapProgresses
                .FirstOrDefaultAsync(p => p.StudentId == userId && p.SkillNodeId == request.NodeId);

            if (progress == null) return (404, "Không tìm thấy tiến độ của kỹ năng này.");

            // Kiểm tra xem có ăn gian (Gọi API hoàn thành Node bị khóa) không
            var validation = await _engineService.ValidatePrerequisiteAsync(userId, request.NodeId);
            if (!validation.IsValid) return (403, "Bạn không thể hoàn thành bài học đang bị khóa.");

            progress.Status = "Completed";
            progress.CompletionPercent = 100;
            progress.CompletedAt = DateTime.Now;
            progress.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return (200, $"Chúc mừng! Đã hoàn thành kỹ năng.");
        }

        public async Task<(int StatusCode, string Message, DashboardSummaryDto? Data)> GetStudentDashboardAsync(Guid userId)
        {
            var progressList = await _context.RoadmapProgresses
                .Include(p => p.SkillNode)
                .Where(p => p.StudentId == userId)
                .ToListAsync();

            if (!progressList.Any()) return (404, "Chưa có lộ trình học tập. Vui lòng tạo lộ trình.", null);

            int totalNodes = progressList.Count;
            int completedNodes = progressList.Count(p => p.Status == "Completed");
            double progressPercent = totalNodes > 0 ? Math.Round(((double)completedNodes / totalNodes) * 100, 2) : 0;

            // Tìm Next Skill (Node có Parent đã học xong nhưng chính nó thì chưa học)
            SkillNodeDto? nextSkillDto = null;
            foreach (var p in progressList.Where(x => x.Status != "Completed"))
            {
                var validation = await _engineService.ValidatePrerequisiteAsync(userId, p.SkillNodeId);
                if (validation.IsValid) // Đủ điều kiện học
                {
                    nextSkillDto = new SkillNodeDto
                    {
                        NodeId = p.SkillNodeId,
                        NodeName = p.SkillNode.NodeName,
                        Description = p.SkillNode.Description
                    };
                    break; // Chỉ lấy 1 node tiếp theo
                }
            }

            var dashboardData = new DashboardSummaryDto
            {
                TotalNodes = totalNodes,
                CompletedNodes = completedNodes,
                ProgressPercentage = progressPercent,
                NextSkill = nextSkillDto,
                TechTrends = new List<TechTrendDto>() // Sẽ xử lý thật ở module Market Analytics
            };

            return (200, "Lấy thông tin tiến độ thành công.", dashboardData);
        }
    }
}