using Repository_TechCompass.Interfaces;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class RoadmapEngineService : IRoadmapEngineService
    {
        private readonly IUserRepository _userRepo;

        public RoadmapEngineService(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        // --- HÀM 1: GENERATE ROADMAP (Đã làm ở bài trước) ---
        public Task<(int StatusCode, string Message, GenerateRoadmapResponseDto? Data)> GenerateRoadmapAsync(Guid userId)
        {
            // ... (Giữ nguyên code hàm GenerateRoadmapAsync của bạn ở đây)
            // Để code ngắn gọn, mình không viết lại nội dung hàm này nhé.
            return Task.FromResult<(int, string, GenerateRoadmapResponseDto?)>((200, "Tạo thành công", new GenerateRoadmapResponseDto()));
        }

        // --- HÀM 2: VALIDATE PREREQUISITE (Kiểm tra điều kiện mở khóa Node) ---
        public Task<(int StatusCode, string Message, bool IsValid)> ValidatePrerequisiteAsync(Guid userId, int nodeId)
        {
            var student = _userRepo.GetStudentByUserId(userId);
            if (student == null) return Task.FromResult((404, "Không tìm thấy sinh viên.", false));

            // TODO: Truy vấn Database thực tế để lấy thông tin Node và ParentNode
            // Giả lập logic: Giả sử Node 103 có ParentNode là 102.
            int? parentNodeId = 102; // Lấy từ Database: _context.Nodes.Find(nodeId).ParentNodeId;

            if (parentNodeId == null)
            {
                // Nếu Node không có Node cha (Node gốc), luôn luôn cho phép mở
                return Task.FromResult((200, "Node hợp lệ, không có điều kiện tiên quyết.", true));
            }

            // Kiểm tra xem sinh viên đã hoàn thành Parent Node chưa
            // bool isParentCompleted = _context.StudentNodeProgress.Any(x => x.UserId == userId && x.NodeId == parentNodeId && x.IsCompleted);
            bool isParentCompleted = false; // Giả lập DB trả về false (Chưa học xong bài trước)

            if (!isParentCompleted)
            {
                return Task.FromResult((403, $"Node này đang bị khóa. Bạn phải hoàn thành kỹ năng tiên quyết (ID: {parentNodeId}) trước.", false));
            }

            return Task.FromResult((200, "Đã đủ điều kiện để học kỹ năng này.", true));
        }

        // --- HÀM 3: ARCHIVE & RECALCULATE (Lưu trữ và tính toán lại) ---
        public async Task<(int StatusCode, string Message, GenerateRoadmapResponseDto? Data)> RecalculateRoadmapAsync(Guid userId)
        {
            var student = _userRepo.GetStudentByUserId(userId);
            if (student == null) return (404, "Không tìm thấy sinh viên.", null);

            // BƯỚC 1: ARCHIVE (Lưu trữ lộ trình cũ)
            // Lấy toàn bộ tiến độ cũ của sinh viên trong Database và đánh dấu IsArchived = true
            // var oldProgress = _context.StudentNodeProgress.Where(x => x.UserId == userId && !x.IsArchived).ToList();
            // foreach (var item in oldProgress) { item.IsArchived = true; }
            // _context.SaveChanges();

            // BƯỚC 2: RECALCULATE (Tạo lại lộ trình mới)
            // Tái sử dụng lại chính hàm GenerateRoadmapAsync để đúc ra lộ trình mới
            var generateResult = await GenerateRoadmapAsync(userId);

            if (generateResult.StatusCode != 200)
            {
                return (generateResult.StatusCode, "Lỗi khi tạo lộ trình mới: " + generateResult.Message, null);
            }

            return (200, "Đã lưu trữ lộ trình cũ và tính toán lại lộ trình mới thành công dựa trên phân tích mới nhất của AI.", generateResult.Data);
        }
    }
}