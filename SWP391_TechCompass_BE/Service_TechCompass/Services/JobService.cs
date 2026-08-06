using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Repository_TechCompass;
// QUAN TRỌNG: Sửa dòng using này cho khớp với nơi bạn lưu file SavedJob.cs ở Bước 1
using Repository_TechCompass.Models;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class JobService : IJobService
    {
        private readonly Swp391CareerRoadmapContext _context;

        public JobService(Swp391CareerRoadmapContext context)
        {
            _context = context;
        }

        public async Task<(bool IsSaved, string Message)> ToggleSaveJobAsync(Guid userId, Guid jobId)
        {
            // Code này không sai, nó chỉ lỗi vì bạn chưa làm xong File 1 và File 2 ở trên
            var existingSavedJob = await _context.SavedJobs
                .FirstOrDefaultAsync(s => s.UserId == userId && s.JobId == jobId);

            if (existingSavedJob != null)
            {
                _context.SavedJobs.Remove(existingSavedJob);
                await _context.SaveChangesAsync();
                return (false, "Đã bỏ lưu việc làm.");
            }
            else
            {
                var newSavedJob = new SavedJob
                {
                    UserId = userId,
                    JobId = jobId,
                    SavedAt = DateTime.UtcNow
                };

                _context.SavedJobs.Add(newSavedJob);
                await _context.SaveChangesAsync();
                return (true, "Lưu việc làm thành công!");
            }
        }
    }
}