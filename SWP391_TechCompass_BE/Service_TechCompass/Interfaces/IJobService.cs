using System;
using System.Threading.Tasks;

namespace Service_TechCompass.Interfaces
{
    public interface IJobService
    {
        // Các hàm cũ của bạn (nếu có)...

        // Hàm xử lý Lưu/Bỏ lưu việc làm
        Task<(bool IsSaved, string Message)> ToggleSaveJobAsync(Guid userId, Guid jobId);
    }
}