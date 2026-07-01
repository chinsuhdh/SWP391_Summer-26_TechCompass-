using Microsoft.EntityFrameworkCore;
using Repository_TechCompass;
using Repository_TechCompass.Models;
using Service_TechCompass.Interfaces;
using System.Threading.Tasks;

namespace Service_TechCompass.Services
{
    public class AdminAnalyticsService : IAdminAnalyticsService
    {
        private readonly Swp391CareerRoadmapContext _context;

        public AdminAnalyticsService(Swp391CareerRoadmapContext context)
        {
            _context = context;
        }

        // 1. Phân tích thị trường (Market Analytics)
        public async Task<object> GetMarketAnalyticsAsync()
        {
            // Code thống kê kỹ năng hot, lộ trình được chọn nhiều nhất...
            return new
            {
                Status = "Success",
                Message = "Lấy dữ liệu phân tích thị trường thành công",
                Data = new { TopSkills = 10, TrendingPaths = 5 } // Thay bằng data thật
            };
        }

        // 2. Hoạt động của sinh viên (Student Activity)
        public async Task<object> GetStudentActivityAsync()
        {
            // Code thống kê sinh viên đang online, tiến độ học tập...
            return new
            {
                Status = "Success",
                Message = "Lấy dữ liệu hoạt động sinh viên thành công",
                Data = new { ActiveStudentsToday = 150, TestsCompleted = 45 } // Thay bằng data thật
            };
        }

        // 3. Thống kê tổng quan (Student Stats) - Cái bạn đang có sẵn
        public async Task<object> GetStudentStatsAsync()
        {
            var totalStudents = await _context.Students.CountAsync();
            return new
            {
                Status = "Success",
                TotalStudents = totalStudents
            };
        }
    }
}