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

namespace Service_TechCompass.Services
{
    public class LearningHubService : ILearningHubService
    {
        private readonly IUserRepository _userRepo;
        private readonly Swp391CareerRoadmapContext _context; // Bổ sung DbContext để query thẳng bảng LearningResources

        // Inject thêm Swp391CareerRoadmapContext vào Constructor
        public LearningHubService(IUserRepository userRepo, Swp391CareerRoadmapContext context)
        {
            _userRepo = userRepo;
            _context = context;
        }

        public async Task<(int StatusCode, string Message, NodeResourcesDto? Data)> GetResourcesByNodeIdAsync(Guid userId, int nodeId)
        {
            // 1. Kiểm tra xem sinh viên có tồn tại hay không
            var student = _userRepo.GetStudentByUserId(userId);
            if (student == null)
            {
                return (404, "Không tìm thấy hồ sơ sinh viên.", null);
            }

            // TODO: Ở hệ thống thật, bạn sẽ check xem sinh viên đã mở khóa (unlock) Node này chưa.
            // Nếu chưa mở khóa thì return lỗi 403 (Không có quyền truy cập bài học).

            // 2. Lấy thông tin Tên Kỹ năng (Node Name) từ Database
            var skillNode = await _context.SkillNodes
                .FirstOrDefaultAsync(n => n.SkillNodeId == nodeId);

            if (skillNode == null)
            {
                return (404, "Không tìm thấy kỹ năng (Node) này trong hệ thống.", null);
            }

            // 3. Query danh sách tài liệu từ bảng learning_resources khớp với nodeId
            var resources = await _context.LearningResources
                .Where(r => r.SkillNodeId == nodeId)
                .Select(r => new ResourceDto
                {
                    ResourceId = r.ResourceId,
                    Title = r.Title,
                    ResourceType = r.ResourceType ?? "Document",
                    Url = r.Url,
                    // Bảng DB chưa có cột Thời lượng (EstimatedMinutes), ta gán giả lập dựa trên loại tài liệu
                    EstimatedMinutes = r.ResourceType == "Video" ? 45 : 20
                })
                .ToListAsync();

            // 4. Map dữ liệu trả về cho Controller
            var responseData = new NodeResourcesDto
            {
                NodeId = nodeId,
                NodeName = skillNode.NodeName,
                Resources = resources
            };

            return (200, "Lấy danh sách tài liệu học tập thành công.", responseData);
        }
    }
}