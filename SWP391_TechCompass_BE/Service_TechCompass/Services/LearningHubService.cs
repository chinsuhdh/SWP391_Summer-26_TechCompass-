using Repository_TechCompass.Interfaces;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class LearningHubService : ILearningHubService
    {
        private readonly IUserRepository _userRepo;

        public LearningHubService(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        public Task<(int StatusCode, string Message, NodeResourcesDto? Data)> GetResourcesByNodeIdAsync(Guid userId, int nodeId)
        {
            var student = _userRepo.GetStudentByUserId(userId);
            if (student == null)
                return Task.FromResult<(int, string, NodeResourcesDto?)>((404, "Không tìm thấy hồ sơ sinh viên.", null));

            // TODO: Ở hệ thống thật, bạn sẽ check xem sinh viên đã mở khóa (unlock) Node này chưa.
            // Nếu chưa mở khóa thì return lỗi 403 (Không có quyền truy cập bài học).

            var resources = new List<ResourceDto>();
            string nodeName = "Kỹ năng lập trình";

            // Mock Data: Giả sử sinh viên bấm vào Node C# Fundamentals (ID 101)
            if (nodeId == 101 || nodeId == 1)
            {
                nodeName = "C# Fundamentals";
                resources.Add(new ResourceDto { ResourceId = 1, Title = "C# Basics for Beginners", ResourceType = "Video", Url = "https://youtube.com/watch?v=mock1", EstimatedMinutes = 45 });
                resources.Add(new ResourceDto { ResourceId = 2, Title = "Microsoft Docs: C# Variables", ResourceType = "Article", Url = "https://learn.microsoft.com/en-us/dotnet/csharp/", EstimatedMinutes = 15 });
            }
            // Mock Data: Giả sử sinh viên bấm vào Node OOP (ID 102)
            else if (nodeId == 102 || nodeId == 2)
            {
                nodeName = "Object-Oriented Programming (OOP) in C#";
                resources.Add(new ResourceDto { ResourceId = 3, Title = "Understanding OOP Concepts", ResourceType = "Video", Url = "https://youtube.com/watch?v=mock2", EstimatedMinutes = 60 });
                resources.Add(new ResourceDto { ResourceId = 4, Title = "Thực hành OOP với 5 bài tập cơ bản", ResourceType = "Quiz", Url = "/quiz/oop-basic", EstimatedMinutes = 30 });
            }
            else
            {
                nodeName = $"Kỹ năng (Node ID: {nodeId})";
                resources.Add(new ResourceDto { ResourceId = 99, Title = "Tài liệu học tập chung", ResourceType = "Document", Url = "https://github.com/mock", EstimatedMinutes = 20 });
            }

            var responseData = new NodeResourcesDto
            {
                NodeId = nodeId,
                NodeName = nodeName,
                Resources = resources
            };

            return Task.FromResult<(int, string, NodeResourcesDto?)>((200, "Lấy danh sách tài liệu học tập thành công.", responseData));
        }
    }
}