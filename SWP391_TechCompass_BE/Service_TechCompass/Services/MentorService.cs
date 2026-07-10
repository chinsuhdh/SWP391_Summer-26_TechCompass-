using Microsoft.EntityFrameworkCore;
using Repository_TechCompass;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Service_TechCompass.Services
{
    public class MentorService : IMentorService
    {
        private readonly Swp391CareerRoadmapContext _context;

        public MentorService(Swp391CareerRoadmapContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<PublicStudentPortfolioDto>> GetPublicPortfoliosAsync(int pageNumber, int pageSize)
        {
            // 1. Tạo Query (Chưa gọi xuống DB)
            var query = _context.EPortfolios
                .Include(p => p.Student)
                    .ThenInclude(s => s.TargetRole)
                .Where(p => p.ShareableUrl != null)
                .AsQueryable();

            // 2. Đếm tổng số bản ghi
            var totalRecords = await query.CountAsync();

            // 3. Lấy đúng 10 bản ghi của trang hiện tại từ Database lên trước
            var rawPortfolios = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 4. Map dữ liệu trên RAM (Client-side evaluation) để tránh lỗi NullReference của Entity Framework
            var portfolios = rawPortfolios.Select(p => new PublicStudentPortfolioDto
            {
                PortfolioId = p.PortfolioId,
                StudentId = p.StudentId,

                // Dùng toán tử ?. để tránh lỗi khi Student hoặc TargetRole bị null
                StudentName = p.Student?.FullName ?? "Sinh viên ẩn danh",
                TargetRole = p.Student?.TargetRole?.RoleName ?? "Chưa rõ định hướng",

                ShareableUrl = p.ShareableUrl,
                AiProfileSummary = p.AiProfileSummary ?? string.Empty
            }).ToList();

            return new PagedResult<PublicStudentPortfolioDto>
            {
                Items = portfolios,
                TotalCount = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<bool> SubmitPortfolioFeedbackAsync(Guid mentorUserId, Guid portfolioId, string reviewNotes)
        {
            // 1. Tìm MentorId dựa trên UserId của người đang đăng nhập
            var mentor = await _context.Mentors.FirstOrDefaultAsync(m => m.UserId == mentorUserId);
            if (mentor == null)
                throw new UnauthorizedAccessException("Tài khoản này không phải là Mentor hợp lệ.");

            // 2. Tìm Portfolio để lấy StudentId
            var portfolio = await _context.EPortfolios.FirstOrDefaultAsync(p => p.PortfolioId == portfolioId);
            if (portfolio == null)
                throw new ArgumentException("Không tìm thấy Portfolio này.");

            // 3. Tạo một MentorSession đại diện cho phiên Review Portfolio này
            var session = new MentorSession
            {
                SessionId = Guid.NewGuid(),
                MentorId = mentor.MentorId,
                StudentId = portfolio.StudentId,
                // SỬA: Thay DateTime.UtcNow thành DateTime.Now
                ScheduledAt = DateTime.Now,
                DurationMinutes = 0,
                Status = "Completed",
                ReviewNotes = reviewNotes,
                PaymentStatus = "Free"
            };

            _context.MentorSessions.Add(session);
            var result = await _context.SaveChangesAsync();

            return result > 0;
        }

        public async Task<List<MentorFeedbackHistoryDto>> GetMentorFeedbackHistoryAsync(Guid mentorUserId)
        {
            // Tìm Mentor dựa vào UserId
            var mentor = await _context.Mentors.FirstOrDefaultAsync(m => m.UserId == mentorUserId);
            if (mentor == null) throw new UnauthorizedAccessException("Tài khoản Mentor không hợp lệ.");

            // Lấy lịch sử đánh giá, join với Student và EPortfolio
            var history = await _context.MentorSessions
                .Include(ms => ms.Student)
                    .ThenInclude(s => s.EPortfolio)
                .Where(ms => ms.MentorId == mentor.MentorId && ms.Status == "Completed")
                .OrderByDescending(ms => ms.ScheduledAt)
                .Select(ms => new MentorFeedbackHistoryDto
                {
                    SessionId = ms.SessionId,
                    StudentName = ms.Student.FullName ?? "Sinh viên ẩn danh",
                    ShareableUrl = ms.Student.EPortfolio.ShareableUrl ?? "",
                    ReviewNotes = ms.ReviewNotes ?? "",
                    ScheduledAt = ms.ScheduledAt
                })
                .ToListAsync();

            return history;
        }
    }
}