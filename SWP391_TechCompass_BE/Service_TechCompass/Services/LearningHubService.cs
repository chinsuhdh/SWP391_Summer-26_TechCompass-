// Service_TechCompass/Services/LearningHubService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Repository_TechCompass;
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
        private readonly Swp391CareerRoadmapContext _context;
        public LearningHubService(IUserRepository userRepo, IContentRepository contentRepo, Swp391CareerRoadmapContext context)
        {
            _userRepo = userRepo;
            _contentRepo = contentRepo;
            _context = context;
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
        // Service_TechCompass/Services/LearningHubService.cs
        public async Task<(int StatusCode, string Message, List<SkillNodeDto>? Data)> GetMyRoadmapAsync(Guid userId)
        {
            var student = _userRepo.GetStudentByUserId(userId);
            if (student == null) return (404, "Không tìm thấy hồ sơ sinh viên.", null);
            if (student.TargetRoleId == null || student.TargetRoleId == 0) return (400, "Chưa định hướng ngành.", null);

            var techPath = _contentRepo.GetAllTechPaths().FirstOrDefault(tp => tp.TargetRoleId == student.TargetRoleId);
            if (techPath == null) return (404, "Chưa có lộ trình chuẩn.", null);

            var nodes = _contentRepo.GetAllSkillNodes()
                .Where(n => n.TechPathId == techPath.TechPathId)
                .OrderBy(n => n.PriorityLevel)
                .ToList();

            // =======================================================
            // FIX: LẤY DANH SÁCH HOT SKILLS TỪ BẢNG TREND_ANALYSIS
            // Giả sử: Kỹ năng có TrendScore >= 3.0 được coi là HOT
            // =======================================================
            var recentHotSkills = _context.TrendAnalyses // (Nhớ inject Swp391CareerRoadmapContext vào Service nhé)
                .Where(t => t.TrendScore >= 3.0m)
                .GroupBy(t => t.SkillNodeId)
                .Select(g => new
                {
                    SkillNodeId = g.Key,
                    MaxScore = g.Max(t => t.TrendScore)
                })
                .ToDictionary(x => x.SkillNodeId, x => x.MaxScore);

            var roadmap = new List<SkillNodeDto>();
            bool isPreviousCompleted = true;

            foreach (var node in nodes)
            {
                var progress = await _contentRepo.GetRoadmapProgressAsync(student.StudentId, node.SkillNodeId);
                bool isCompleted = progress != null && progress.Status.Equals("completed", StringComparison.OrdinalIgnoreCase);
                bool isLocked = !isPreviousCompleted && progress == null;

                // KIỂM TRA XEM NODE CÓ NẰM TRONG DICTIONARY HOT SKILL KHÔNG
                bool isTrending = recentHotSkills.ContainsKey(node.SkillNodeId);
                decimal trendScore = isTrending ? recentHotSkills[node.SkillNodeId].GetValueOrDefault() : 0;

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
                    IsLocked = isLocked,

                    // GÁN GIÁ TRỊ VÀO DTO ĐỂ TRẢ VỀ FRONTEND
                    IsTrending = isTrending,
                    CurrentTrendScore = trendScore
                });

                isPreviousCompleted = isCompleted;
            }

            return (200, "Lấy lộ trình học tập thành công.", roadmap);
        }
    }
}