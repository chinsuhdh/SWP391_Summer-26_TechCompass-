using Repository_TechCompass.Interfaces;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class RoadmapService : IRoadmapService
    {
        private readonly IUserRepository _userRepo;

        public RoadmapService(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        // TÍNH NĂNG 1: HIỂN THỊ CÂY KỸ NĂNG (TECH PATH MAP)
        public Task<(int StatusCode, string Message, List<SkillNodeDto>? Data)> GetSkillTreeAsync(Guid userId)
        {
            // TODO: Thay bằng query lấy từ CSDL: bảng Nodes join với bảng Student_Node_Progress
            // Dưới đây là Mock Data để Frontend có thể vẽ biểu đồ cây ngay lập tức
            var skillTree = new List<SkillNodeDto>
            {
                new SkillNodeDto { NodeId = 1, NodeName = "C# Basic", Description = "Biến, Vòng lặp, Câu lệnh rẽ nhánh", ParentNodeId = null, IsCompleted = true, IsLocked = false },
                new SkillNodeDto { NodeId = 2, NodeName = "OOP in C#", Description = "Tính đóng gói, Kế thừa, Đa hình", ParentNodeId = 1, IsCompleted = true, IsLocked = false },
                new SkillNodeDto { NodeId = 3, NodeName = "SQL Server", Description = "Thiết kế CSDL, Query căn bản", ParentNodeId = 1, IsCompleted = false, IsLocked = false },
                new SkillNodeDto { NodeId = 4, NodeName = "Entity Framework Core", Description = "ORM, Migration, LINQ", ParentNodeId = 2, IsCompleted = false, IsLocked = true }, // Bị khóa vì chưa học xong C# Basic
                new SkillNodeDto { NodeId = 5, NodeName = "ASP.NET Core Web API", Description = "RESTful API, Middleware", ParentNodeId = 4, IsCompleted = false, IsLocked = true }
            };

            return Task.FromResult<(int, string, List<SkillNodeDto>?)>((200, "Lấy sơ đồ Roadmap thành công", skillTree));
        }

        // TÍNH NĂNG 2: ĐÁNH DẤU HOÀN THÀNH NODE
        public Task<(int StatusCode, string Message)> MarkNodeCompletedAsync(Guid userId, MarkNodeCompletedDto request)
        {
            var student = _userRepo.GetStudentByUserId(userId);
            if (student == null) return Task.FromResult<(int, string)>((404, "Không tìm thấy thông tin sinh viên"));

            // TODO: Tìm Node trong CSDL và thêm/cập nhật bản ghi vào bảng Student_Node_Progress thành Completed
            // var progress = _context.StudentNodeProgress.FirstOrDefault(x => x.UserId == userId && x.NodeId == request.NodeId);
            // progress.IsCompleted = true;
            // _context.SaveChanges();

            return Task.FromResult<(int, string)>((200, $"Chúc mừng! Bạn đã hoàn thành kỹ năng Node ID: {request.NodeId}. Tiếp tục phát huy nhé!"));
        }

        // TÍNH NĂNG 3: DASHBOARD TỔNG HỢP (TIẾN ĐỘ, NEXT SKILL, TRENDS)
        public Task<(int StatusCode, string Message, DashboardSummaryDto? Data)> GetStudentDashboardAsync(Guid userId)
        {
            var student = _userRepo.GetStudentByUserId(userId);
            if (student == null) return Task.FromResult<(int, string, DashboardSummaryDto?)>((404, "Không tìm thấy hồ sơ sinh viên.", null));

            // TODO: Lấy dữ liệu thật từ Database
            // 1. Tính toán Roadmap Progress
            int totalNodes = 25; // Giả sử Roadmap có 25 bài học
            int completedNodes = 5;
            double progress = Math.Round(((double)completedNodes / totalNodes) * 100, 2);

            // 2. Xác định Next Skill (Kỹ năng đầu tiên chưa hoàn thành và không bị khóa)
            var nextSkill = new SkillNodeDto
            {
                NodeId = 3,
                NodeName = "SQL Server",
                Description = "Bạn cần hoàn thành CSDL trước khi sang Entity Framework"
            };

            // 3. Lấy Trends Công nghệ (Có thể kết hợp gọi AI API sau này)
            var trends = new List<TechTrendDto>
            {
                new TechTrendDto { TrendName = "Generative AI", TrendDescription = "Tích hợp LLMs vào ứng dụng", PopularityScore = 98 },
                new TechTrendDto { TrendName = ".NET 8 Performance", TrendDescription = "Tối ưu hóa API cực mạnh", PopularityScore = 85 },
                new TechTrendDto { TrendName = "Microservices", TrendDescription = "Kiến trúc hệ thống lớn", PopularityScore = 90 }
            };

            var dashboardData = new DashboardSummaryDto
            {
                TotalNodes = totalNodes,
                CompletedNodes = completedNodes,
                ProgressPercentage = progress,
                NextSkill = nextSkill,
                TechTrends = trends
            };

            return Task.FromResult<(int, string, DashboardSummaryDto?)>((200, "Tải dữ liệu Dashboard thành công.", dashboardData));
        }
    }
}