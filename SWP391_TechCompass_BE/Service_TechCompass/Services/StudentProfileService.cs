// src/Service_TechCompass/Services/StudentProfileService.cs
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs;
using Service_TechCompass.Hubs;
using Service_TechCompass.Interfaces;
using UglyToad.PdfPig;

namespace Service_TechCompass.Services
{
    public class StudentProfileService : IStudentProfileService
    {
        private readonly IUserRepository _userRepo;
        private readonly IChatCompletionService _chatCompletionService;
        private readonly IHubContext<RoadmapNotificationHub> _hubContext; // ĐÃ THÊM: SignalR Hub

        // ĐÃ THÊM: Inject IHubContext vào Constructor
        public StudentProfileService(
            IUserRepository userRepo,
            Kernel kernel,
            IHubContext<RoadmapNotificationHub> hubContext)
        {
            _userRepo = userRepo;
            _chatCompletionService = kernel.GetRequiredService<IChatCompletionService>("GeminiChat");
            _hubContext = hubContext;
        }

        public Task<(int StatusCode, string Message, UserStudentProfileDto? Data)> GetProfileAsync(Guid userId)
        {
            var user = _userRepo.GetUserById(userId);
            var student = _userRepo.GetStudentByUserId(userId);

            if (user == null || student == null)
            {
                return Task.FromResult<(int, string, UserStudentProfileDto?)>((404, "Không tìm thấy hồ sơ người dùng.", null));
            }

            var profileData = new UserStudentProfileDto
            {
                UserId = user.UserId,
                Email = user.Email,
                FullName = student.FullName,
                StudentCode = student.StudentCode,
                LatentTalentSummary = student.LatentTalentSummary,
                TargetRoleId = student.TargetRoleId,
                UpdatedAt = student.UpdatedAt
            };

            return Task.FromResult<(int, string, UserStudentProfileDto?)>((200, "Lấy thông tin thành công.", profileData));
        }

        // ĐÃ SỬA: Đổi thành async Task để có thể await SignalR
        public async Task<(int StatusCode, string Message)> UpdateProfileAsync(Guid userId, UpdateStudentProfileDto request)
        {
            var student = _userRepo.GetStudentByUserId(userId);
            if (student == null)
            {
                return (404, "Không tìm thấy hồ sơ sinh viên để cập nhật.");
            }

            // ĐÃ THÊM: Kiểm tra xem Target Role có bị thay đổi không
            bool isRoleChanged = student.TargetRoleId != request.TargetRoleId;

            student.FullName = request.FullName;
            student.StudentCode = string.IsNullOrWhiteSpace(request.StudentCode) ? null : request.StudentCode.Trim();
            student.LatentTalentSummary = request.LatentTalentSummary;
            student.TargetRoleId = request.TargetRoleId;
            student.UpdatedAt = DateTime.Now;

            try
            {
                _userRepo.UpdateStudent(student);
                _userRepo.SaveChanges();

                // ĐÃ THÊM: Bắn tín hiệu Real-time nếu Role thay đổi
                if (isRoleChanged)
                {
                    await _hubContext.Clients.Group($"roadmap_user_{userId}").SendAsync("TargetRoleChanged", new
                    {
                        message = "Định hướng nghề nghiệp đã thay đổi, hệ thống đang tự động cập nhật lại Lộ trình học tập."
                    });
                }

                return (200, "Cập nhật hồ sơ thành công.");
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException != null && ex.InnerException.Message.Contains("UQ_students_student_code"))
                {
                    return (400, "Mã số học viên này đã được sử dụng bởi một tài khoản khác. Vui lòng kiểm tra lại.");
                }

                return (500, "Lỗi hệ thống khi lưu dữ liệu. Vui lòng thử lại sau.");
            }
        }

        public async Task<(int StatusCode, string Message, object? Data)> ProcessTranscriptAsync(Guid userId, IFormFile file)
        {
            var student = _userRepo.GetStudentByUserId(userId);
            if (student == null)
            {
                return (404, "Không tìm thấy sinh viên trong hệ thống.", null);
            }

            try
            {
                string extractedText = string.Empty;

                // 1. Đọc nội dung file PDF thành Text bằng UglyToad.PdfPig
                using (var stream = file.OpenReadStream())
                {
                    using (var document = PdfDocument.Open(stream))
                    {
                        foreach (var page in document.GetPages())
                        {
                            extractedText += page.Text + "\n";
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    return (400, "Không thể đọc được chữ từ file PDF. Đảm bảo đây không phải là file ảnh PDF được scan.", null);
                }

                // 2. Dùng AI (Gemini) để bóc tách thông tin thành JSON
                var chatHistory = new ChatHistory();
                chatHistory.AddSystemMessage(@"Bạn là hệ thống phân tích học bạ sinh viên chuyên ngành Software Engineering. 
Nhiệm vụ của bạn là trích xuất các môn học liên quan đến lập trình, IT (đặc biệt chú ý các mã môn học như PRN, SWD, PRJ, v.v.), và điểm số tương ứng.
Bạn PHẢI trả về dữ liệu ĐÚNG định dạng JSON sau, không kèm bất kỳ markdown (như ```json) hay giải thích nào khác:
{
  ""extractedSubjects"": [
    { ""subjectCode"": ""Mã môn"", ""subjectName"": ""Tên môn"", ""score"": Điểm(số thực) }
  ],
  ""latentTalentAnalysis"": ""Đoạn văn ngắn 3-4 câu nhận xét thế mạnh của sinh viên dựa trên điểm các môn học cốt lõi (ví dụ: tư duy logic tốt nếu điểm toán/thuật toán cao, hoặc kỹ năng thực hành tốt nếu điểm project cao).""
}");
                chatHistory.AddUserMessage($"Đây là nội dung bảng điểm:\n{extractedText}");

                var response = await _chatCompletionService.GetChatMessageContentAsync(chatHistory);
                string jsonResponse = response.ToString() ?? "";

                // Làm sạch chuỗi JSON phòng trường hợp AI vẫn trả về thẻ markdown
                jsonResponse = jsonResponse.Replace("```json", "").Replace("```", "").Trim();

                // 3. Phân tích JSON và cập nhật vào Database
                using var jsonDoc = JsonDocument.Parse(jsonResponse);
                var root = jsonDoc.RootElement;

                string aiSummary = root.TryGetProperty("latentTalentAnalysis", out var summaryElement)
                                    ? summaryElement.GetString() ?? ""
                                    : "";

                // Cập nhật nhận xét của AI vào trường LatentTalentSummary của Student
                student.LatentTalentSummary = string.IsNullOrEmpty(student.LatentTalentSummary)
                                                ? aiSummary
                                                : student.LatentTalentSummary + "\n" + aiSummary;

                student.UpdatedAt = DateTime.Now;
                _userRepo.UpdateStudent(student);
                _userRepo.SaveChanges();

                // Chuyển chuỗi JSON thành object vô danh để trả về cho Client dễ đọc
                var resultData = JsonSerializer.Deserialize<object>(jsonResponse);

                return (200, "Phân tích bảng điểm thành công.", resultData);
            }
            catch (JsonException jsonEx)
            {
                return (500, "Lỗi phân tích cú pháp JSON từ AI: " + jsonEx.Message, null);
            }
            catch (Exception ex)
            {
                return (500, $"Lỗi hệ thống khi xử lý file: {ex.Message}", null);
            }
        }
    }
}