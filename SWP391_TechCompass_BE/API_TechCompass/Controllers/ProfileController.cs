using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repository_TechCompass;
using Service_TechCompass.DTOs;
using Microsoft.EntityFrameworkCore;
using Service_TechCompass.Interfaces;

namespace API_TechCompass.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Yêu cầu bắt buộc phải có JWT Token mới được gọi API này
    public class ProfileController : ControllerBase
    {
        private readonly IStudentProfileService _profileService;

        public ProfileController(IStudentProfileService profileService)
        {
            _profileService = profileService;
        }

        // Lấy ID người dùng hiện tại từ Token một cách an toàn
        private Guid GetCurrentUserId()
        {
            // ASP.NET Core mặc định map "sub" thành ClaimTypes.NameIdentifier
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            // Thêm dòng fallback ở trên để đảm bảo bắt được mọi trường hợp

            return string.IsNullOrEmpty(userIdClaim) ? Guid.Empty : Guid.Parse(userIdClaim);
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = GetCurrentUserId();
            var result = await _profileService.GetProfileAsync(userId);

            if (result.StatusCode != 200)
            {
                return StatusCode(result.StatusCode, new { message = result.Message });
            }

            return Ok(new { message = result.Message, data = result.Data });
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateStudentProfileDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var result = await _profileService.UpdateProfileAsync(userId, request);

            if (result.StatusCode != 200)
            {
                return StatusCode(result.StatusCode, new { message = result.Message });
            }

            return Ok(new { message = result.Message });
        }

        [HttpPost("upload-transcript")]
        [Consumes("multipart/form-data")] 
        public async Task<IActionResult> UploadTranscript(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Vui lòng chọn file bảng điểm hợp lệ." });
            }

            // Validate định dạng file (ví dụ: chỉ nhận PDF)
            if (Path.GetExtension(file.FileName).ToLower() != ".pdf")
            {
                return BadRequest(new { message = "Hệ thống hiện chỉ hỗ trợ định dạng PDF." });
            }

            var userId = GetCurrentUserId();
            var result = await _profileService.ProcessTranscriptAsync(userId, file);

            if (result.StatusCode != 200)
            {
                return StatusCode(result.StatusCode, new { message = result.Message });
            }

            return Ok(new { message = result.Message, extractedData = result.Data });
        }

        [HttpGet("target-roles")]
        [AllowAnonymous] // Cho phép gọi không cần token (nếu cần) hoặc bỏ dòng này đi
        public async Task<IActionResult> GetTargetRoles([FromServices] Swp391CareerRoadmapContext context)
        {
            try
            {
                var roles = await context.TargetCareerRoles
                    .Select(r => new {
                        id = r.TargetRoleId,
                        name = r.RoleName
                    })
                    .ToListAsync();

                return Ok(new { message = "Lấy danh sách ngành nghề thành công", data = roles });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tải danh sách ngành nghề", detail = ex.Message });
            }
        }
    }
}