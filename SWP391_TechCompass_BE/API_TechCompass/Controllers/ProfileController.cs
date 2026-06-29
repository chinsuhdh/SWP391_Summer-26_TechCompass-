using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;
using Service_TechCompass.Services;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API_TechCompass.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Bắt buộc phải login mới gọi được API này
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;

        public ProfileController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            try
            {
                // Trích xuất UserId từ Token Claim của người đang đăng nhập
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
                                  ?? User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized("Token không hợp lệ hoặc đã hết hạn!");
                }

                Guid userId = Guid.Parse(userIdClaim);

                // Gọi xuống lớp tầng nghiệp vụ (Service) xử lý phân quyền lấy dữ liệu
                var profile = await _profileService.GetProfileMeAsync(userId);

                return Ok(profile);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}