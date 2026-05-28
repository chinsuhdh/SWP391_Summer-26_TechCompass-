using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Repository_TechCompass.Models;
using Repository_TechCompass.Repositories;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepo;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;

        public AuthService(IUserRepository userRepo, IEmailService emailService, IConfiguration config)
        {
            _userRepo = userRepo;
            _emailService = emailService;
            _config = config;
        }

        public async Task<(int StatusCode, string Message)> RegisterAsync(RegisterDto request)
        {
            if (_userRepo.EmailExists(request.Email))
            {
                return (400, "Email đã tồn tại trong hệ thống.");
            }

            Random rand = new Random();
            string otp = rand.Next(100000, 999999).ToString();

            var newUser = new User
            {
                UserId = Guid.NewGuid(),
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Provider = "Email",
                IsActive = false,
                CreatedAt = DateTime.Now,
                RoleId = 2,
                OtpCode = otp,
                OtpExpiry = DateTime.Now.AddMinutes(10)
            };

            _userRepo.AddUser(newUser);

            var newStudent = new Student
            {
                StudentId = Guid.NewGuid(),
                UserId = newUser.UserId,
                FullName = request.FullName,
                UpdatedAt = DateTime.Now
            };

            _userRepo.AddStudent(newStudent);
            _userRepo.SaveChanges();

            string subject = "TechCompass - Mã xác thực tài khoản mới";
            string body = $"<h3>Chào {request.FullName},</h3><p>Cảm ơn bạn đã tham gia TechCompass. Để kích hoạt tài khoản, vui lòng nhập mã OTP dưới đây:</p><p><b style='color:green; font-size: 24px;'>{otp}</b></p><p>Mã này sẽ hết hạn trong 10 phút.</p>";

            await _emailService.SendEmailAsync(newUser.Email, subject, body);

            return (200, "Đăng ký thành công! Vui lòng kiểm tra email để lấy mã xác thực.");
        }

        public (int StatusCode, string Message) VerifyAccount(VerifyAccountDto request)
        {
            var user = _userRepo.GetUserByEmail(request.Email);
            if (user == null) return (404, "Email không tồn tại trong hệ thống.");
            if (user.IsActive == true) return (400, "Tài khoản này đã được xác thực từ trước.");
            if (user.OtpCode != request.OtpCode) return (400, "Mã OTP không chính xác.");
            if (DateTime.Now > user.OtpExpiry) return (400, "Mã OTP đã hết hạn. Vui lòng yêu cầu gửi lại.");

            user.IsActive = true;
            user.OtpCode = null;
            user.OtpExpiry = null;

            _userRepo.SaveChanges();

            return (200, "Xác thực tài khoản thành công! Bạn có thể đăng nhập ngay bây giờ.");
        }

        public (int StatusCode, string Message, string Token) Login(LoginDto request)
        {
            var user = _userRepo.GetUserByEmail(request.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return (401, "Sai email hoặc mật khẩu.", string.Empty);
            }

            if (user.IsActive == false)
            {
                if (user.OtpCode != null)
                    return (403, "Tài khoản chưa được xác thực. Vui lòng kiểm tra email và nhập mã OTP.", string.Empty);

                return (403, "Tài khoản của bạn đã bị khóa.", string.Empty);
            }

            var token = GenerateJwtToken(user);
            return (200, "Đăng nhập thành công!", token);
        }

        public async Task<(int StatusCode, string Message)> ForgotPasswordAsync(ForgotPasswordDto request)
        {
            var user = _userRepo.GetUserByEmail(request.Email);
            if (user == null) return (404, "Không tìm thấy email trong hệ thống.");

            Random rand = new Random();
            string otp = rand.Next(100000, 999999).ToString();

            user.OtpCode = otp;
            user.OtpExpiry = DateTime.Now.AddMinutes(5);
            _userRepo.SaveChanges();

            string subject = "TechCompass - Mã xác nhận lấy lại mật khẩu";
            string body = $"<h3>Chào bạn,</h3><p>Mã OTP để đặt lại mật khẩu của bạn là: <b style='color:blue; font-size: 20px;'>{otp}</b></p><p>Mã này sẽ hết hạn trong 5 phút.</p>";

            await _emailService.SendEmailAsync(user.Email, subject, body);

            return (200, "Mã OTP đã được gửi đến email của bạn.");
        }

        public (int StatusCode, string Message) ResetPassword(VerifyOtpAndResetPasswordDto request)
        {
            var user = _userRepo.GetUserByEmail(request.Email);
            if (user == null) return (404, "Email không hợp lệ.");
            if (user.OtpCode != request.OtpCode) return (400, "Mã OTP không chính xác.");
            if (DateTime.Now > user.OtpExpiry) return (400, "Mã OTP đã hết hạn. Vui lòng yêu cầu gửi lại.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.OtpCode = null;
            user.OtpExpiry = null;

            _userRepo.SaveChanges();

            return (200, "Đặt lại mật khẩu thành công! Bạn có thể đăng nhập ngay bây giờ.");
        }

        private string GenerateJwtToken(User user)
        {
            var jwtConfig = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("RoleId", user.RoleId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtConfig["Issuer"],
                audience: jwtConfig["Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(Convert.ToDouble(jwtConfig["ExpireDays"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}