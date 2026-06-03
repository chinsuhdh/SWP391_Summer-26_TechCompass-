using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Repository_TechCompass;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class RoadmapEngineService : IRoadmapEngineService
    {
        private readonly IUserRepository _userRepo;
        private readonly Swp391CareerRoadmapContext _context;

        public RoadmapEngineService(IUserRepository userRepo, Swp391CareerRoadmapContext context)
        {
            _userRepo = userRepo;
            _context = context;
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
    }
}