using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service_TechCompass.Interfaces;

namespace API_TechCompass.Controllers
{
    // Cấu hình đường dẫn gốc: /api/v1/jobs
    [Route("api/v1/jobs")]
    [ApiController]
    public class JobController : ControllerBase
    {
        private readonly IJobService _jobService;

        public JobController(IJobService jobService)
        {
            _jobService = jobService;
        }

        // Chức năng: Lưu / Bỏ lưu việc làm (Frontend truyền jobId lên qua URL)
        [HttpPost("{jobId}/save")]
        [Authorize] // Bắt buộc user phải có Token đăng nhập hợp lệ
        public async Task<IActionResult> ToggleSaveJob(Guid jobId)
        {
            try
            {
                // Lấy UserId của người đang đăng nhập từ Token (JWT)
                var userIdString = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value
                                ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdString))
                {
                    return Unauthorized(new { Message = "Không xác định được người dùng. Vui lòng đăng nhập lại." });
                }

                var userId = Guid.Parse(userIdString);

                // Gọi Service để xử lý logic Thêm/Xóa khỏi DB
                var (isSaved, message) = await _jobService.ToggleSaveJobAsync(userId, jobId);

                // Trả về kết quả cho ReactJS biết nút sẽ sáng hay tắt
                return Ok(new
                {
                    Message = message,
                    IsSaved = isSaved
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống khi lưu việc làm.", Error = ex.Message });
            }
        }
    }
}