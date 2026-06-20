// Service_TechCompass/Services/LearningHubService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class LearningHubService : ILearningHubService
    {
        private readonly IUserRepository _userRepo;
        private readonly IContentRepository _contentRepo;

        public LearningHubService(IUserRepository userRepo, IContentRepository contentRepo)
        {
            _userRepo = userRepo;
            _contentRepo = contentRepo;
        }

        public async Task<(int StatusCode, string Message, NodeResourcesDto? Data)> GetResourcesByNodeIdAsync(Guid userId, int nodeId)
        {
            // ... (Giữ nguyên code cũ của bạn)
            var student = _userRepo.GetStudentByUserId(userId);
            if (student == null) return (404, "Không tìm thấy hồ sơ sinh viên.", null);

            var skillNode = await _contentRepo.GetSkillNodeByIdAsync(nodeId);
            if (skillNode == null) return (404, "Không tìm thấy kỹ năng (Node) này trong hệ thống.", null);

            var existingProgress = await _contentRepo.GetRoadmapProgressAsync(student.StudentId, nodeId);
            bool isEnrolled = existingProgress != null;
            string currentStatus = existingProgress != null ? existingProgress.Status : "not-started";

            var rawResources = await _contentRepo.GetLearningResourcesByNodeIdAsync(nodeId);
            var resourceDtos = rawResources.Select(r => new ResourceDto
            {
                ResourceId = r.ResourceId,
                Title = r.Title,
                ResourceType = r.ResourceType ?? "Document",
                Url = r.Url,
                EstimatedMinutes = r.ResourceType == "Video" ? 45 : 20,
                IsRegistered = isEnrolled,
                Status = currentStatus
            }).ToList();

            var responseData = new NodeResourcesDto
            {
                NodeId = nodeId,
                NodeName = skillNode.NodeName,
                Resources = resourceDtos
            };

            return (200, "Lấy danh sách tài liệu học tập thành công.", responseData);
        }

        public async Task<(int StatusCode, string Message)> EnrollResourceAsync(Guid userId, int resourceId)
        {
            // ... (Giữ nguyên code cũ của bạn)
            var student = _userRepo.GetStudentByUserId(userId);
            if (student == null) return (404, "Không tìm thấy hồ sơ sinh viên.");

            var resource = await _contentRepo.GetLearningResourceByIdAsync(resourceId);
            if (resource == null) return (404, "Tài nguyên học tập không tồn tại.");

            int skillNodeId = resource.SkillNodeId;
            if (skillNodeId == 0) return (400, "Dữ liệu bài học bị lỗi (Không thuộc kỹ năng nào).");

            var existingProgress = await _contentRepo.GetRoadmapProgressAsync(student.StudentId, skillNodeId);
            if (existingProgress != null) return (400, "Bạn đã đăng ký học phần kỹ năng này rồi!");

            var newProgress = new RoadmapProgress
            {
                ProgressId = Guid.NewGuid(),
                StudentId = student.StudentId,
                SkillNodeId = skillNodeId,
                Status = "learning",
                CompletionPercent = 0,
                StartedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var learningHistory = new LearningHistory
            {
                HistoryId = Guid.NewGuid(),
                ProgressId = newProgress.ProgressId,
                ActionType = "ENROLL",
                DurationSeconds = 0,
                RecordedAt = DateTime.UtcNow
            };

            newProgress.LearningHistories.Add(learningHistory);
            await _contentRepo.AddRoadmapProgressAsync(newProgress);
            _contentRepo.SaveChanges();

            return (200, "Đăng ký khóa học thành công.");
        }

        // ==========================================
        // CẬP NHẬT: HÀM LẤY LỘ TRÌNH ĐỘNG THEO NGÀNH CỦA SINH VIÊN
        // ==========================================
        public async Task<(int StatusCode, string Message, List<SkillNodeDto>? Data)> GetMyRoadmapAsync(Guid userId)
        {
            var student = _userRepo.GetStudentByUserId(userId);
            if (student == null)
            {
                return (404, "Không tìm thấy hồ sơ sinh viên.", null);
            }

            // 1. Sinh viên chưa chọn ngành (Bước vào lần đầu)
            if (student.TargetRoleId == null || student.TargetRoleId == 0)
            {
                return (400, "Sinh viên chưa định hướng ngành nghề. Vui lòng chọn ngành nghề trước.", null);
            }

            // 2. Tìm TechPath tương ứng với Role của sinh viên
            var techPath = _contentRepo.GetAllTechPaths().FirstOrDefault(tp => tp.TargetRoleId == student.TargetRoleId);

            if (techPath == null)
            {
                return (404, "Hệ thống chưa có lộ trình chuẩn cho ngành nghề bạn chọn.", null);
            }

            // 3. Lấy đúng các Node thuộc về ngành nghề đó
            var nodes = _contentRepo.GetAllSkillNodes()
                .Where(n => n.TechPathId == techPath.TechPathId)
                .OrderBy(n => n.PriorityLevel)
                .ToList();

            var roadmap = new List<SkillNodeDto>();
            bool isPreviousCompleted = true; // Node đầu tiên mặc định luôn mở

            foreach (var node in nodes)
            {
                // Khớp tiến độ thực tế (Đã được Assessment ghi nhận)
                var progress = await _contentRepo.GetRoadmapProgressAsync(student.StudentId, node.SkillNodeId);

                // Dùng StringComparison.OrdinalIgnoreCase để tránh lỗi chữ hoa/chữ thường trong DB ("completed" vs "Completed")
                bool isCompleted = progress != null && progress.Status.Equals("completed", StringComparison.OrdinalIgnoreCase);
                bool isLocked = !isPreviousCompleted && progress == null;

                roadmap.Add(new SkillNodeDto
                {
                    SkillNodeId = node.SkillNodeId,
                    TechPathId = node.TechPathId,
                    ParentNodeId = node.ParentNodeId,
                    NodeName = node.NodeName,
                    Description = node.Description,
                    PriorityLevel = node.PriorityLevel,

                    NodeId = node.SkillNodeId,
                    IsCompleted = isCompleted,
                    IsLocked = isLocked
                });

                isPreviousCompleted = isCompleted;
            }

            return (200, "Lấy lộ trình học tập thành công.", roadmap);
        }
    }
}