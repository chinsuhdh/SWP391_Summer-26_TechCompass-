using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class AdminContentService : IAdminContentService
    {
        private readonly IContentRepository _contentRepo;

        public AdminContentService(IContentRepository contentRepo)
        {
            _contentRepo = contentRepo;
        }

        #region TECH PATH CRUD
        public Task<(int StatusCode, string Message, List<TechPathDto>? Data)> GetAllTechPathsAsync()
        {
            var data = _contentRepo.GetAllTechPaths().Select(x => new TechPathDto
            {
                TechPathId = x.TechPathId,
                TargetRoleId = x.TargetRoleId,
                PathName = x.PathName,
                Description = x.Description,
                TotalNodes = x.TotalNodes
            }).ToList();
            return Task.FromResult<(int, string, List<TechPathDto>?)>((200, "Thành công", data));
        }

        public Task<(int StatusCode, string Message, TechPathDto? Data)> GetTechPathByIdAsync(int id)
        {
            var path = _contentRepo.GetTechPathById(id);
            if (path == null) return Task.FromResult<(int, string, TechPathDto?)>((404, "Không tìm thấy Tech Path.", null));

            var data = new TechPathDto
            {
                TechPathId = path.TechPathId,
                TargetRoleId = path.TargetRoleId,
                PathName = path.PathName,
                Description = path.Description,
                TotalNodes = path.TotalNodes
            };
            return Task.FromResult<(int, string, TechPathDto?)>((200, "Thành công", data));
        }

        public Task<(int StatusCode, string Message)> CreateTechPathAsync(CreateUpdateTechPathDto request)
        {
            var newPath = new TechPath
            {
                PathName = request.PathName,
                Description = request.Description,
                TargetRoleId = request.TargetRoleId
            };
            _contentRepo.AddTechPath(newPath);
            _contentRepo.SaveChanges();
            return Task.FromResult<(int, string)>((201, "Tạo Tech Path thành công."));
        }

        public Task<(int StatusCode, string Message)> UpdateTechPathAsync(int id, CreateUpdateTechPathDto request)
        {
            var path = _contentRepo.GetTechPathById(id);
            if (path == null) return Task.FromResult<(int, string)>((404, "Không tìm thấy Tech Path để cập nhật."));

            path.PathName = request.PathName;
            path.Description = request.Description;
            path.TargetRoleId = request.TargetRoleId;

            _contentRepo.UpdateTechPath(path);
            _contentRepo.SaveChanges();
            return Task.FromResult<(int, string)>((200, "Cập nhật Tech Path thành công."));
        }

        public Task<(int StatusCode, string Message)> DeleteTechPathAsync(int id)
        {
            var path = _contentRepo.GetTechPathById(id);
            if (path == null) return Task.FromResult<(int, string)>((404, "Không tìm thấy Tech Path để xóa."));

            _contentRepo.DeleteTechPath(path);
            _contentRepo.SaveChanges();
            return Task.FromResult<(int, string)>((200, "Xóa Tech Path thành công."));
        }
        #endregion

        #region SKILL NODE CRUD
        public Task<(int StatusCode, string Message, SkillNodeDto? Data)> GetSkillNodeByIdAsync(int id)
        {
            var node = _contentRepo.GetSkillNodeById(id);
            if (node == null) return Task.FromResult<(int, string, SkillNodeDto?)>((404, "Không tìm thấy Skill Node.", null));

            var data = new SkillNodeDto
            {
                SkillNodeId = node.SkillNodeId,
                TechPathId = node.TechPathId,
                ParentNodeId = node.ParentNodeId,
                NodeName = node.NodeName,
                Description = node.Description,
                PriorityLevel = node.PriorityLevel
            };
            return Task.FromResult<(int, string, SkillNodeDto?)>((200, "Thành công", data));
        }

        public Task<(int StatusCode, string Message, List<SkillNodeDto>? Data)> GetAllSkillNodesAsync()
        {
            var data = _contentRepo.GetAllSkillNodes().Select(x => new SkillNodeDto
            {
                SkillNodeId = x.SkillNodeId,
                TechPathId = x.TechPathId,
                ParentNodeId = x.ParentNodeId,
                NodeName = x.NodeName,
                Description = x.Description,
                PriorityLevel = x.PriorityLevel
            }).ToList();
            return Task.FromResult<(int, string, List<SkillNodeDto>?)>((200, "Thành công", data));
        }

        public Task<(int StatusCode, string Message)> CreateSkillNodeAsync(CreateUpdateSkillNodeDto request)
        {
            var newNode = new SkillNode
            {
                NodeName = request.NodeName,
                Description = request.Description,
                TechPathId = request.PathId,
                ParentNodeId = request.ParentNodeId,
                PriorityLevel = request.EstimatedDays
            };
            _contentRepo.AddSkillNode(newNode);
            _contentRepo.SaveChanges();
            return Task.FromResult<(int, string)>((201, "Tạo Skill Node thành công."));
        }

        public Task<(int StatusCode, string Message)> UpdateSkillNodeAsync(int id, CreateUpdateSkillNodeDto request)
        {
            var node = _contentRepo.GetSkillNodeById(id);
            if (node == null) return Task.FromResult<(int, string)>((404, "Không tìm thấy Skill Node để cập nhật."));

            node.NodeName = request.NodeName;
            node.Description = request.Description;
            node.TechPathId = request.PathId;
            node.ParentNodeId = request.ParentNodeId;
            node.PriorityLevel = request.EstimatedDays;

            _contentRepo.UpdateSkillNode(node);
            _contentRepo.SaveChanges();
            return Task.FromResult<(int, string)>((200, "Cập nhật Skill Node thành công."));
        }

        public Task<(int StatusCode, string Message)> DeleteSkillNodeAsync(int id)
        {
            var node = _contentRepo.GetSkillNodeById(id);
            if (node == null) return Task.FromResult<(int, string)>((404, "Không tìm thấy Skill Node để xóa."));

            _contentRepo.DeleteSkillNode(node);
            _contentRepo.SaveChanges();
            return Task.FromResult<(int, string)>((200, "Xóa Skill Node thành công."));
        }
        #endregion

        #region LEARNING RESOURCE CRUD
        public Task<(int StatusCode, string Message, List<LearningResourceDto>? Data)> GetAllLearningResourcesAsync()
        {
            var data = _contentRepo.GetAllLearningResources().Select(x => new LearningResourceDto
            {
                ResourceId = x.ResourceId,
                SkillNodeId = x.SkillNodeId,
                Title = x.Title,
                Url = x.Url,
                ResourceType = x.ResourceType,
                Provider = x.Provider,
                DifficultyLevel = x.DifficultyLevel
            }).ToList();
            return Task.FromResult<(int, string, List<LearningResourceDto>?)>((200, "Thành công", data));
        }

        public Task<(int StatusCode, string Message, LearningResourceDto? Data)> GetLearningResourceByIdAsync(int id)
        {
            var res = _contentRepo.GetLearningResourceById(id);
            if (res == null) return Task.FromResult<(int, string, LearningResourceDto?)>((404, "Không tìm thấy Learning Resource.", null));

            var data = new LearningResourceDto
            {
                ResourceId = res.ResourceId,
                SkillNodeId = res.SkillNodeId,
                Title = res.Title,
                Url = res.Url,
                ResourceType = res.ResourceType,
                Provider = res.Provider,
                DifficultyLevel = res.DifficultyLevel
            };
            return Task.FromResult<(int, string, LearningResourceDto?)>((200, "Thành công", data));
        }

        public Task<(int StatusCode, string Message)> CreateLearningResourceAsync(CreateUpdateLearningResourceDto request)
        {
            var newRes = new LearningResource
            {
                SkillNodeId = request.NodeId,
                Title = request.Title,
                Url = request.Url,
                ResourceType = request.ResourceType,
                Provider = request.Provider,
                DifficultyLevel = request.DifficultyLevel
            };
            _contentRepo.AddResource(newRes);
            _contentRepo.SaveChanges();
            return Task.FromResult<(int, string)>((201, "Tạo Learning Resource thành công."));
        }

        public Task<(int StatusCode, string Message)> UpdateLearningResourceAsync(int id, CreateUpdateLearningResourceDto request)
        {
            var res = _contentRepo.GetLearningResourceById(id);
            if (res == null) return Task.FromResult<(int, string)>((404, "Không tìm thấy Learning Resource để cập nhật."));

            res.SkillNodeId = request.NodeId;
            res.Title = request.Title;
            res.Url = request.Url;
            res.ResourceType = request.ResourceType;
            res.Provider = request.Provider;
            res.DifficultyLevel = request.DifficultyLevel;

            _contentRepo.UpdateResource(res);
            _contentRepo.SaveChanges();
            return Task.FromResult<(int, string)>((200, "Cập nhật Learning Resource thành công."));
        }

        public Task<(int StatusCode, string Message)> DeleteLearningResourceAsync(int id)
        {
            var res = _contentRepo.GetLearningResourceById(id);
            if (res == null) return Task.FromResult<(int, string)>((404, "Không tìm thấy Learning Resource để xóa."));

            _contentRepo.DeleteResource(res);
            _contentRepo.SaveChanges();
            return Task.FromResult<(int, string)>((200, "Xóa Learning Resource thành công."));
        }
        #endregion

        #region ROADMAP GITHUB SYNC
        public async Task<(int StatusCode, string Message)> SyncRoadmapFromGitHubAsync(string rawUrl, int targetRoleId)
        {
            try
            {
                // 1. Tải JSON từ GitHub Raw
                using var client = new HttpClient();
                var jsonResponse = await client.GetStringAsync(rawUrl);

                // Config bỏ qua phân biệt hoa thường khi parse JSON
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var roadmapData = JsonSerializer.Deserialize<RoadmapShDto>(jsonResponse, options);

                if (roadmapData == null || roadmapData.Items == null)
                    return (400, "Lỗi đọc file JSON hoặc định dạng không khớp.");

                // 2. Tạo TechPath mới (Bộ khung gốc)
                var techPath = new TechPath
                {
                    PathName = roadmapData.Title ?? "Imported Roadmap",
                    Description = roadmapData.Description ?? "Dữ liệu được đồng bộ từ Github",
                    TargetRoleId = targetRoleId,
                    TotalNodes = 0
                };

                _contentRepo.AddTechPath(techPath);
                _contentRepo.SaveChanges(); // Lưu để sinh ra TechPathId tự động

                // 3. Hàm đệ quy để lưu SkillNodes (Móc nối ID Cha - Con)
                int totalNodesAdded = TraverseAndSaveNodes(roadmapData.Items, techPath.TechPathId, null, 1);

                // Cập nhật lại tổng số node
                techPath.TotalNodes = totalNodesAdded;
                _contentRepo.UpdateTechPath(techPath);
                _contentRepo.SaveChanges();

                return (200, $"Đồng bộ thành công! Đã tạo nhánh {techPath.PathName} với {totalNodesAdded} module.");
            }
            catch (Exception ex)
            {
                return (500, $"Lỗi hệ thống khi đồng bộ: {ex.Message}");
            }
        }

        // Đổi thành hàm đồng bộ (sync) vì Repository pattern của bạn đang dùng SaveChanges() đồng bộ
        private int TraverseAndSaveNodes(List<RoadmapNodeDto> nodes, int techPathId, int? parentNodeId, int priority)
        {
            if (nodes == null || !nodes.Any()) return 0;
            int count = 0;

            foreach (var item in nodes)
            {
                var skillNode = new SkillNode
                {
                    TechPathId = techPathId,
                    NodeName = item.Name ?? "Unnamed Node",
                    Description = item.Description ?? "Đang cập nhật tài liệu",
                    ParentNodeId = parentNodeId,
                    PriorityLevel = priority,
                    IsCodingRequired = false
                };

                _contentRepo.AddSkillNode(skillNode);
                _contentRepo.SaveChanges(); // Cần Save ngay để EF Core cấp Identity ID cho ParentNodeId đời sau
                count++;

                // Nếu có node con, đệ quy gọi lại và truyền ID của node hiện tại làm Parent
                if (item.Children != null && item.Children.Any())
                {
                    count += TraverseAndSaveNodes(item.Children, techPathId, skillNode.SkillNodeId, priority + 1);
                }
            }
            return count;
        }
        #endregion
    }
}