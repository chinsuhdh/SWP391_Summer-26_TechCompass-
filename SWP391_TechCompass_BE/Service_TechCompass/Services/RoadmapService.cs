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
            if (student == null)
            {
                return (404, "Không tìm thấy hồ sơ sinh viên.", null);
            }

            var query = from p in _context.RoadmapProgresses
                        join n in _context.SkillNodes on p.SkillNodeId equals n.SkillNodeId
                        where p.StudentId == student.StudentId
                        orderby n.PriorityLevel ascending
                        select new { p, n };

            var studentNodes = await query.ToListAsync();
            var skillTree = new List<SkillNodeDto>();

            foreach (var item in studentNodes)
            {
                var validation = await _engineService.ValidatePrerequisiteAsync(student.StudentId, item.n.SkillNodeId);

                skillTree.Add(new SkillNodeDto
                {
                    NodeId = item.n.SkillNodeId,
                    NodeName = item.n.NodeName,
                    Description = item.n.Description,
                    ParentNodeId = item.n.ParentNodeId,
                    IsCompleted = (item.p.Status == "Completed"),
                    IsLocked = !validation.IsValid
                });
            }

            return (200, "Lấy sơ đồ Roadmap thành công.", skillTree);
        }

        public async Task<(int StatusCode, string Message)> MarkNodeCompletedAsync(Guid userId, MarkNodeCompletedDto request)
        {
            var student = _userRepo.GetStudentByUserId(userId);
            if (student == null)
            {
                return (404, "Không tìm thấy hồ sơ sinh viên.");
            }

            // 1. Kiểm tra điều kiện tiên quyết (Có bị khóa không?)
            var validation = await _engineService.ValidatePrerequisiteAsync(student.StudentId, request.NodeId);
            if (!validation.IsValid)
            {
                return (403, "Bạn không thể hoàn thành bài học đang bị khóa.");
            }

            // 2. Tìm tiến độ hiện tại trong DB
            var progress = await _context.RoadmapProgresses
                .FirstOrDefaultAsync(p => p.StudentId == student.StudentId && p.SkillNodeId == request.NodeId);

            // 3. LOGIC MỚI: Nếu chưa có tiến độ (chưa từng học), TỰ ĐỘNG TẠO MỚI thay vì báo lỗi 404
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
                // Nếu đã có (đang học dở), thì cập nhật thành Completed
                progress.Status = "Completed";
                progress.CompletionPercent = 100;
                progress.CompletedAt = DateTime.Now;
                progress.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            // 4. Bắn tín hiệu Real-time về Frontend để UI tự update màu xanh
            await _hubContext.Clients.Group($"roadmap_user_{userId}").SendAsync("ReceiveRoadmapUpdate", new
            {
                NodeId = request.NodeId,
                Status = "Completed",
                Message = "Kỹ năng đã được cập nhật mở khóa."
            });

            return (200, "Chúc mừng! Đã hoàn thành kỹ năng.");
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