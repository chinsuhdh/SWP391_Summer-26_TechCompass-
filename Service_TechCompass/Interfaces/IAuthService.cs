using Service_TechCompass.DTOs;

namespace Service_TechCompass.Services
{
    public interface IAuthService
    {
        Task<(int StatusCode, string Message)> RegisterAsync(RegisterDto request);
        (int StatusCode, string Message) VerifyAccount(VerifyAccountDto request);
        (int StatusCode, string Message, string Token) Login(LoginDto request);
        Task<(int StatusCode, string Message)> ForgotPasswordAsync(ForgotPasswordDto request);
        (int StatusCode, string Message) ResetPassword(VerifyOtpAndResetPasswordDto request);
    }
}