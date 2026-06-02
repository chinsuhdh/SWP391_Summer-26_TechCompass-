using Repository_TechCompass.Interfaces;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service_TechCompass.Services
{
    public class AdminMonitorService : IAdminMonitorService
    {
        private readonly IUserRepository _userRepo;

        public AdminMonitorService(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        public Task<(int StatusCode, string Message, List<AiRecommendationDto>? Data)> GetAllAiRecommendationsAsync()
        {
            var data = _userRepo.GetAllAiRecommendations().Select(x => new AiRecommendationDto
            {
                RecommendationId = x.RecommendationId,
                StudentId = x.StudentId,
                RecommendationType = x.RecommendationType,
                ContentJson = x.ContentJson,
                GeneratedAt = x.GeneratedAt
            }).ToList();

            return Task.FromResult<(int, string, List<AiRecommendationDto>?)>((200, "Lấy dữ liệu AI Recommendations thành công.", data));
        }

        public Task<(int StatusCode, string Message, List<SystemLogDto>? Data)> GetSystemLogsAsync()
        {
            // Trả về dữ liệu log mô phỏng. Nếu muốn, bạn có thể tạo bảng system_logs trong CSDL sau.
            var logs = new List<SystemLogDto>
            {
                new SystemLogDto { LogId = Guid.NewGuid(), LogLevel = "INFO", Message = "Hệ thống khởi động thành công.", CreatedAt = DateTime.Now.AddHours(-2) },
                new SystemLogDto { LogId = Guid.NewGuid(), LogLevel = "INFO", Message = "Sinh viên đã hoàn thành Test AI.", CreatedAt = DateTime.Now.AddHours(-1) },
                new SystemLogDto { LogId = Guid.NewGuid(), LogLevel = "WARNING", Message = "API LLM phản hồi chậm hơn 5 giây.", CreatedAt = DateTime.Now.AddMinutes(-30) },
                new SystemLogDto { LogId = Guid.NewGuid(), LogLevel = "ERROR", Message = "Lỗi kết nối khi quét Job Market.", CreatedAt = DateTime.Now.AddMinutes(-5) }
            };

            return Task.FromResult<(int, string, List<SystemLogDto>?)>((200, "Lấy danh sách System Logs thành công.", logs));
        }
    }
}