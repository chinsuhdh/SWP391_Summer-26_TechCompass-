using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace API_TechCompass.Controllers
{
    [Route("api/career")]
    [ApiController]
    [Authorize] // Bắt buộc sinh viên phải đăng nhập (Có JWT Token)
    public class CareerController : ControllerBase
    {
        private readonly ICareerService _careerService;

        public CareerController(ICareerService careerService)
        {
            _careerService = careerService;
        }

        // Hàm lấy ID của sinh viên đang đăng nhập
        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return string.IsNullOrEmpty(userIdClaim) ? Guid.Empty : Guid.Parse(userIdClaim);
        }

        // POST: api/career/survey
        // Sinh viên nộp khảo sát
        [HttpPost("survey")]
        public async Task<IActionResult> SubmitSurvey([FromBody] SubmitSurveyDto request)
        {
            var userId = GetCurrentUserId();
            var res = await _careerService.SubmitSurveyAsync(userId, request);

            if (res.StatusCode != 200) return StatusCode(res.StatusCode, new { message = res.Message });

            return Ok(new { message = res.Message, aiAnalysis = res.AnalyzedResult });
        }

        // PUT: api/career/target-role
        // Sinh viên chốt chọn nghề nghiệp
        [HttpPut("target-role")]
        public async Task<IActionResult> SelectTargetRole([FromBody] SelectTargetRoleDto request)
        {
            var userId = GetCurrentUserId();
            var res = await _careerService.SelectTargetRoleAsync(userId, request);

            if (res.StatusCode != 200) return StatusCode(res.StatusCode, new { message = res.Message });

            return Ok(new { message = res.Message });
        }
    }
}