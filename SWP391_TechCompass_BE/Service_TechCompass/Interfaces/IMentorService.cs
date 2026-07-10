using Service_TechCompass.DTOs;
using System;
using System.Threading.Tasks;

namespace Service_TechCompass.Interfaces
{
    public interface IMentorService
    {
        // Lấy danh sách Portfolio công khai cho Mentor xem
        Task<PagedResult<PublicStudentPortfolioDto>> GetPublicPortfoliosAsync(int pageNumber, int pageSize);

        // Mentor gửi nhận xét cho Portfolio của sinh viên
        Task<bool> SubmitPortfolioFeedbackAsync(Guid mentorUserId, Guid portfolioId, string reviewNotes);


        // Thêm khai báo hàm:
        Task<List<MentorFeedbackHistoryDto>> GetMentorFeedbackHistoryAsync(Guid mentorUserId);
    }
}