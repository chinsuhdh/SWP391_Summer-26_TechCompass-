using Service_TechCompass.DTOs;
using Microsoft.AspNetCore.Mvc;
using Service_TechCompass.Services;

namespace API_TechCompass.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto request)
        {
            var result = await _authService.RegisterAsync(request);
            if (result.StatusCode != 200) return StatusCode(result.StatusCode, new { message = result.Message });
            return Ok(new { message = result.Message });
        }

        [HttpPost("verify-account")]
        public IActionResult VerifyAccount([FromBody] VerifyAccountDto request)
        {
            var result = _authService.VerifyAccount(request);
            if (result.StatusCode != 200) return StatusCode(result.StatusCode, new { message = result.Message });
            return Ok(new { message = result.Message });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto request)
        {
            var result = _authService.Login(request);
            if (result.StatusCode != 200) return StatusCode(result.StatusCode, new { message = result.Message });
            return Ok(new { token = result.Token, message = result.Message });
        }

        // ĐÂY LÀ ENDPOINT GOOGLE CẦN THÊM VÀO
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto request)
        {
            var result = await _authService.GoogleLoginAsync(request);
            if (result.StatusCode != 200) return StatusCode(result.StatusCode, new { message = result.Message });
            return Ok(new { token = result.Token, message = result.Message });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            return Ok(new { message = "Đăng xuất thành công. Vui lòng xóa token ở phía client." });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto request)
        {
            var result = await _authService.ForgotPasswordAsync(request);
            if (result.StatusCode != 200) return StatusCode(result.StatusCode, new { message = result.Message });
            return Ok(new { message = result.Message });
        }

        [HttpPost("reset-password")]
        public IActionResult ResetPassword([FromBody] VerifyOtpAndResetPasswordDto request)
        {
            var result = _authService.ResetPassword(request);
            if (result.StatusCode != 200) return StatusCode(result.StatusCode, new { message = result.Message });
            return Ok(new { message = result.Message });
        }
    }
}