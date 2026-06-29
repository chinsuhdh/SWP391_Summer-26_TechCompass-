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
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;

        public ProfileController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        // API 1: Lấy hồ sơ cá nhân
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            try
            {
                Guid userId = GetUserIdFromToken();
                var profile = await _profileService.GetProfileMeAsync(userId);
                return Ok(profile);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // API 2: Cập nhật thông tin sinh viên
        [HttpPut("update")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateStudentProfileDto dto)
        {
            try
            {
                Guid userId = GetUserIdFromToken();
                var result = await _profileService.UpdateStudentProfileAsync(userId, dto);
                return Ok(new { message = "Cập nhật thông tin hồ sơ thành công!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // API 3: Lấy chi tiết sâu của sinh viên
        [HttpGet("student-detail")]
        public async Task<IActionResult> GetStudentDetail()
        {
            try
            {
                Guid userId = GetUserIdFromToken();
                var result = await _profileService.GetStudentProfileOnlyAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Hàm dùng chung để bóc tách UserId từ Token mã hóa
        private Guid GetUserIdFromToken()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
                              ?? User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                throw new Exception("Phiên đăng nhập không hợp lệ hoặc hết hạn!");
            }

            return Guid.Parse(userIdClaim);
        }
    }
}