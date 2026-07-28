// src/Service_TechCompass/Services/AuthService.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

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
            // 1. Kiểm tra xem Email đã tồn tại chưa
            if (_userRepo.EmailExists(request.Email))
            {
                return (400, "Email đã tồn tại trong hệ thống.");
            }

            // 2. Tạo mã OTP kích hoạt tài khoản
            Random rand = new Random();
            string otp = rand.Next(100000, 999999).ToString();

            // 3. Khởi tạo thực thể User (Bảng cha)
            var newUser = new User
            {
                UserId = Guid.NewGuid(),
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Provider = "Email",
                IsActive = false, // Sẽ kích hoạt sau khi xác thực OTP thành công
                CreatedAt = DateTime.Now,
                RoleId = 2, // Mặc định tự đăng ký qua màn hình Register sẽ là Student (Role 2)
                OtpCode = otp,
                OtpExpiry = DateTime.Now.AddMinutes(10)
            };

            _userRepo.AddUser(newUser);

            // 4. Khởi tạo hồ sơ Student mở rộng (Bảng con)
            var newStudent = new Student
            {
                StudentId = Guid.NewGuid(),
                UserId = newUser.UserId, // Liên kết khóa ngoại quan hệ 1-1 trỏ tới bảng User
                FullName = request.FullName,
                UpdatedAt = DateTime.Now,
                StudentCode = "SE" + rand.Next(100000, 999999).ToString() // Sinh mã số sinh viên tự động tránh lỗi DB
            };

            _userRepo.AddStudent(newStudent);

            // 5. Lưu đồng thời 2 bảng xuống Database
            _userRepo.SaveChanges();

            // 6. Gửi Email chứa mã OTP về hòm thư người dùng
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

            // Kích hoạt tài khoản và xóa mã OTP cũ
            user.IsActive = true;
            user.OtpCode = null;
            user.OtpExpiry = null;

            _userRepo.SaveChanges();

            return (200, "Xác thực tài khoản thành công! Bạn có thể đăng nhập ngay bây giờ.");
        }

        public (int StatusCode, string Message, string Token) Login(LoginDto request)
        {
            var user = _userRepo.GetUserByEmail(request.Email);

            // Kiểm tra thông tin tài khoản và băm verify mật khẩu qua BCrypt
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return (401, "Sai email hoặc mật khẩu.", string.Empty);
            }

            // Kiểm tra xem tài khoản đã được kích hoạt OTP hoặc có bị khóa hay không
            if (user.IsActive == false)
            {
                if (user.OtpCode != null)
                    return (403, "Tài khoản chưa được xác thực. Vui lòng kiểm tra email và nhập mã OTP.", string.Empty);

                return (403, "Tài khoản của bạn đã bị khóa.", string.Empty);
            }

            // Tạo mã JWT Token trả về cho Client lưu LocalStorage
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

            // Cập nhật mật khẩu mới và băm bảo mật lại bằng BCrypt
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.OtpCode = null;
            user.OtpExpiry = null;

            _userRepo.SaveChanges();

            return (200, "Đặt lại mật khẩu thành công! Bạn có thể đăng nhập ngay bây giờ.");
        }

        public async Task<(int StatusCode, string Message, string Token)> GoogleLoginAsync(GoogleLoginDto request)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings() { };
                var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);

                if (payload == null)
                {
                    return (401, "Google Token không hợp lệ.", string.Empty);
                }

                var user = _userRepo.GetUserByEmail(payload.Email);

                // Nếu tài khoản Google đăng nhập lần đầu -> Tiến hành tự động đăng ký
                if (user == null)
                {
                    user = new User
                    {
                        UserId = Guid.NewGuid(),
                        Email = payload.Email,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // Sinh pass ngẫu nhiên phòng hờ
                        Provider = "Google",
                        IsActive = true, // Google login mặc định tin cậy nên kích hoạt luôn
                        CreatedAt = DateTime.Now,
                        RoleId = 2,
                    };

                    _userRepo.AddUser(user);

                    var newStudent = new Student
                    {
                        StudentId = Guid.NewGuid(),
                        UserId = user.UserId,
                        FullName = payload.Name,
                        UpdatedAt = DateTime.Now,
                        StudentCode = "SE" + new Random().Next(100000, 999999).ToString() // Đồng bộ fix lỗi Unique cho đăng nhập Google
                    };

                    _userRepo.AddStudent(newStudent);
                    _userRepo.SaveChanges();
                }
                else
                {
                    if (user.IsActive == false)
                    {
                        user.IsActive = true;
                        user.OtpCode = null;
                        user.OtpExpiry = null;
                        _userRepo.SaveChanges();
                    }
                }

                var token = GenerateJwtToken(user);
                return (200, "Đăng nhập bằng Google thành công!", token);
            }
            catch (InvalidJwtException)
            {
                return (400, "Token Google đã hết hạn hoặc bị giả mạo.", string.Empty);
            }
            catch (Exception ex)
            {
                return (500, $"Lỗi hệ thống: {ex.Message}", string.Empty);
            }
        }

        // =========================================================================================
        // [BẢO MẬT TUYỆT ĐỐI]: ĐĂNG NHẬP VÀ LIÊN KẾT BẰNG GITHUB OAUTH
        // =========================================================================================
        public async Task<(int StatusCode, string Message, string Token)> GithubLoginAsync(GithubLoginDto request)
        {
            try
            {
                var githubConfig = _config.GetSection("GithubOAuth");
                string clientId = githubConfig["ClientId"]!;
                string clientSecret = githubConfig["ClientSecret"]!;

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"client_id", clientId},
                    {"client_secret", clientSecret},
                    {"code", request.Code},
                    {"redirect_uri", "http://localhost:5173/login"}
                });

                var tokenResponse = await httpClient.PostAsync("https://github.com/login/oauth/access_token", tokenRequest);
                var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
                var tokenDoc = JsonDocument.Parse(tokenJson);

                if (!tokenDoc.RootElement.TryGetProperty("access_token", out var accessTokenElement))
                {
                    return (401, "Xác thực GitHub thất bại. Không lấy được quyền truy cập.", string.Empty);
                }

                string accessToken = accessTokenElement.GetString()!;

                var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
                userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                userRequest.Headers.UserAgent.Add(new ProductInfoHeaderValue("TechCompassApp", "1.0"));

                var userResponse = await httpClient.SendAsync(userRequest);
                var userJson = await userResponse.Content.ReadAsStringAsync();
                var userDoc = JsonDocument.Parse(userJson);

                string githubUsername = userDoc.RootElement.GetProperty("login").GetString()!;
                string name = userDoc.RootElement.TryGetProperty("name", out var nameProp) && nameProp.ValueKind != JsonValueKind.Null
                              ? nameProp.GetString()! : githubUsername;


                var emailRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
                emailRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                emailRequest.Headers.UserAgent.Add(new ProductInfoHeaderValue("TechCompassApp", "1.0"));

                var emailResponse = await httpClient.SendAsync(emailRequest);
                var emailJson = await emailResponse.Content.ReadAsStringAsync();
                var emailDoc = JsonDocument.Parse(emailJson);

                string realEmail = null;

                foreach (var element in emailDoc.RootElement.EnumerateArray())
                {
                    if (element.GetProperty("primary").GetBoolean() && element.GetProperty("verified").GetBoolean())
                    {
                        realEmail = element.GetProperty("email").GetString();
                        break;
                    }
                }

                string email = realEmail ?? $"{githubUsername}@users.noreply.github.com";
                // ---------------------------------------------------------

                var user = _userRepo.GetUserByEmail(email);


                // 3. Nếu chưa có tài khoản -> Tự tạo tài khoản & Gắn chặt GithubUsername
                if (user == null)
                {
                    user = new User
                    {
                        UserId = Guid.NewGuid(),
                        Email = email,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // Random pass
                        Provider = "GitHub",
                        IsActive = true, // Trust GitHub OAuth
                        CreatedAt = DateTime.Now,
                        RoleId = 2,
                    };
                    _userRepo.AddUser(user);

                    var newStudent = new Student
                    {
                        StudentId = Guid.NewGuid(),
                        UserId = user.UserId,
                        FullName = name,
                        UpdatedAt = DateTime.Now,
                        StudentCode = "SE" + new Random().Next(100000, 999999).ToString(),
                        GithubUsername = githubUsername // <--- BẢO MẬT: Lấy trực tiếp từ hệ thống GitHub
                    };
                    _userRepo.AddStudent(newStudent);
                    _userRepo.SaveChanges();
                }
                else
                {
                    // Nếu đã có tài khoản -> Cập nhật trạng thái và gắn GithubUsername nếu chưa có
                    if (user.IsActive == false)
                    {
                        user.IsActive = true;
                        user.OtpCode = null;
                        user.OtpExpiry = null;
                    }

                    var student = _userRepo.GetStudentByUserId(user.UserId);
                    if (student != null && string.IsNullOrEmpty(student.GithubUsername))
                    {
                        student.GithubUsername = githubUsername; // <--- TỰ ĐỘNG GẮN TÊN CHÍNH CHỦ VÀO HỒ SƠ
                        student.UpdatedAt = DateTime.Now;
                    }
                    _userRepo.SaveChanges();
                }

                var token = GenerateJwtToken(user);
                return (200, "Đăng nhập bằng GitHub thành công!", token);
            }
            catch (Exception ex)
            {
                return (500, $"Lỗi hệ thống: {ex.Message}", string.Empty);
            }
        }

        private string GenerateJwtToken(User user)
        {
            var jwtConfig = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // 1. Dịch chuyển ID sang Tên vai trò (Role Name) tương ứng để thiết lập Authorization
            var roleName = user.RoleId switch
            {
                1 => "Admin",
                2 => "Student",
                3 => "Mentor",
                4 => "Counselor",
                _ => "User"
            };

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("RoleId", user.RoleId.ToString()),
                new Claim(ClaimTypes.Role, roleName), // Gán claim role để fix lỗi 403 Authorization ở FE/BE
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // 2. Tự động kiểm tra vai trò và Inject ID thực thể của bảng con vào Claim phục vụ cho frontend gọi API profile
            switch (user.RoleId)
            {
                case 1: // Admin đặc quyền
                    claims.Add(new Claim("FullName", "System Administrator"));
                    break;

                case 2: // Sinh viên học tập
                    var student = _userRepo.GetStudentByUserId(user.UserId);
                    if (student != null)
                    {
                        claims.Add(new Claim("StudentId", student.StudentId.ToString()));
                        claims.Add(new Claim("FullName", student.FullName ?? ""));
                    }
                    break;

                case 3: // Mentor doanh nghiệp
                    var mentor = _userRepo.GetMentorByUserId(user.UserId);
                    if (mentor != null)
                    {
                        claims.Add(new Claim("MentorId", mentor.MentorId.ToString()));
                        claims.Add(new Claim("FullName", mentor.FullName ?? ""));
                    }
                    break;

                case 4: // Cố vấn tư vấn học đường
                    var counselor = _userRepo.GetCounselorByUserId(user.UserId);
                    if (counselor != null)
                    {
                        claims.Add(new Claim("CounselorId", counselor.CounselorId.ToString()));
                        claims.Add(new Claim("FullName", counselor.FullName ?? ""));
                    }
                    break;
            }

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