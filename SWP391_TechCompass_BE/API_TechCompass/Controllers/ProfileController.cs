using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;
using System;
using System.Linq;
using System.Security.Claims; // BẮT BUỘC PHẢI CÓ THƯ VIỆN NÀY
using System.Threading.Tasks;

namespace API_TechCompass.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;

        public ProfileController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMyProfile()
        {
            // 1. Quét tìm UserId ở mọi định dạng có thể có trong Token
            var userIdString = User.Claims.FirstOrDefault(c =>
                c.Type == "UserId" ||
                c.Type == "sub" ||
                c.Type == ClaimTypes.NameIdentifier)?.Value;

            // 2. Quét tìm RoleId
            var roleIdString = User.Claims.FirstOrDefault(c => c.Type == "RoleId")?.Value;

            // DỰ PHÒNG: Nếu Token không lưu số RoleId, mà lưu chữ "Admin" hoặc "Student"
            if (string.IsNullOrEmpty(roleIdString))
            {
                var roleName = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role")?.Value;
                if (roleName == "Admin") roleIdString = "1";
                else if (roleName == "Student") roleIdString = "2";
            }

            // Kiểm tra chốt chặn
            if (string.IsNullOrEmpty(userIdString) || string.IsNullOrEmpty(roleIdString))
                return Unauthorized("Không tìm thấy UserId hoặc RoleId trong Token. Hãy thử đăng nhập lại.");

            var userId = Guid.Parse(userIdString);
            var roleId = int.Parse(roleIdString);

            // 3. Gọi Service
            var profile = await _profileService.GetProfileMeAsync(userId, roleId);

            if (profile == null) return NotFound("Không tìm thấy hồ sơ người dùng.");

            return Ok(profile);
        }

        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateStudentProfileDto dto)
        {
            try
            {
                var userIdString = User.Claims.FirstOrDefault(c => c.Type == "UserId" || c.Type == "sub" || c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

                var userId = Guid.Parse(userIdString);
                var success = await _profileService.UpdateProfileMeAsync(userId, dto);

                if (success) return Ok("Cập nhật hồ sơ thành công!");
                return BadRequest("Cập nhật thất bại, không có thay đổi nào.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("upload-transcript")]
        [Authorize]
        public async Task<IActionResult> UploadTranscript(IFormFile file)
        {
            try
            {
                var userIdString = User.Claims.FirstOrDefault(c => c.Type == "UserId" || c.Type == "sub" || c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

                var userId = Guid.Parse(userIdString);
                var url = await _profileService.UploadTranscriptAsync(userId, file);
                return Ok(new { Url = url });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("target-roles")]
        public async Task<IActionResult> GetTargetRoles()
        {
            var roles = await _profileService.GetTargetRolesAsync();
            return Ok(roles);
        }
    }
}