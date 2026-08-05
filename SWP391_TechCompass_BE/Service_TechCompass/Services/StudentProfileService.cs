using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Google.GenAI.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Repository_TechCompass;
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
        private readonly IHubContext<RoadmapNotificationHub> _hubContext;
        private readonly Swp391CareerRoadmapContext _context;

        public StudentProfileService(
            IUserRepository userRepo,
            Kernel kernel,
            IHubContext<RoadmapNotificationHub> hubContext,
            Swp391CareerRoadmapContext context)
        {
            _userRepo = userRepo;
            _chatCompletionService = kernel.GetRequiredService<IChatCompletionService>("GeminiChat");
            _hubContext = hubContext;
            _context = context;
        }

        public async Task<(int StatusCode, string Message, object? Data)> GetProfileAsync(Guid userId)
        {
            var user = await _userRepo.GetUserByIdAsync(userId);
            if (user == null)
            {
                return (404, "Không tìm thấy tài khoản người dùng.", null);
            }

            string roleName = user.RoleId switch
            {
                1 => "Admin",
                2 => "Student",
                3 => "Mentor",
                4 => "Counselor",
                _ => "User"
            };

            switch (user.RoleId)
            {
                case 1: // Admin
                    return (200, "Lấy thông tin Admin thành công.", new
                    {
                        UserId = user.UserId,
                        Email = user.Email,
                        RoleId = user.RoleId,
                        RoleName = roleName,
                        FullName = "System Administrator"
                    });

                case 2: // Student
                    var student = await _userRepo.GetStudentByUserIdAsync(userId);
                    if (student == null) return (404, "Không tìm thấy hồ sơ sinh viên.", null);

                    return (200, "Lấy thông tin Sinh viên thành công.", new
                    {
                        UserId = user.UserId,
                        Email = user.Email,
                        RoleId = user.RoleId,
                        RoleName = roleName,
                        FullName = student.FullName,
                        StudentCode = student.StudentCode,
                        LatentTalentSummary = student.LatentTalentSummary,
                        TargetRoleId = student.TargetRoleId,
                        UpdatedAt = student.UpdatedAt
                    });

                case 3: // Mentor
                    var mentor = await _userRepo.GetMentorByUserIdAsync(userId);
                    if (mentor == null) return (404, "Không tìm thấy hồ sơ Mentor.", null);

                    return (200, "Lấy thông tin Mentor thành công.", new
                    {
                        UserId = user.UserId,
                        Email = user.Email,
                        RoleId = user.RoleId,
                        RoleName = roleName,
                        FullName = mentor.FullName,
                        ExpertiseTags = mentor.ExpertiseTags,
                        CurrentCompany = mentor.CurrentCompany,
                        LinkedinUrl = mentor.LinkedinUrl
                    });

                case 4: // Counselor
                    var counselor = await _userRepo.GetCounselorByUserIdAsync(userId);
                    if (counselor == null) return (404, "Không tìm thấy hồ sơ Counselor.", null);

                    return (200, "Lấy thông tin Counselor thành công.", new
                    {
                        UserId = user.UserId,
                        Email = user.Email,
                        RoleId = user.RoleId,
                        RoleName = roleName,
                        FullName = counselor.FullName,
                        Department = counselor.Department,
                        UpdatedAt = counselor.UpdatedAt
                    });

                default:
                    return (200, "Lấy thông tin thành công.", new
                    {
                        UserId = user.UserId,
                        Email = user.Email,
                        RoleId = user.RoleId,
                        RoleName = roleName,
                        FullName = "Người dùng ẩn danh"
                    });
            }
        }

        public async Task<(int StatusCode, string Message)> UpdateProfileAsync(Guid userId, UpdateStudentProfileDto request)
        {
            var student = _userRepo.GetStudentByUserId(userId);
            if (student == null)
            {
                return (404, "Không tìm thấy hồ sơ sinh viên để cập nhật.");
            }

            string newStudentCode = string.IsNullOrWhiteSpace(request.StudentCode) ? null : request.StudentCode.Trim();

            if (!string.IsNullOrEmpty(newStudentCode))
            {
                bool isCodeTaken = _userRepo.IsStudentCodeExists(newStudentCode, student.StudentId);
                if (isCodeTaken)
                {
                    return (400, "Mã số học viên này đã được sử dụng bởi một tài khoản khác. Vui lòng kiểm tra lại.");
                }
            }

            bool isRoleChanged = student.TargetRoleId != request.TargetRoleId;

            student.FullName = request.FullName;
            student.StudentCode = newStudentCode;
            student.LatentTalentSummary = request.LatentTalentSummary;
            student.TargetRoleId = request.TargetRoleId;
            student.UpdatedAt = DateTime.Now;

            try
            {
                _userRepo.UpdateStudent(student);
                _userRepo.SaveChanges();

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

                jsonResponse = jsonResponse.Replace("```json", "").Replace("```", "").Trim();

                using var jsonDoc = JsonDocument.Parse(jsonResponse);
                var root = jsonDoc.RootElement;

                string aiSummary = root.TryGetProperty("latentTalentAnalysis", out var summaryElement)
                                    ? summaryElement.GetString() ?? ""
                                    : "";

                student.LatentTalentSummary = string.IsNullOrEmpty(student.LatentTalentSummary)
                                                ? aiSummary
                                                : student.LatentTalentSummary + "\n" + aiSummary;

                student.UpdatedAt = DateTime.Now;
                _userRepo.UpdateStudent(student);
                _userRepo.SaveChanges();

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

        public async Task<(int StatusCode, string Message, List<StudentFeedbackDto>? Data)> GetMyFeedbacksAsync(Guid userId)
        {
            try
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
                if (student == null)
                    return (404, "Không tìm thấy hồ sơ sinh viên.", null);

                var feedbacks = await _context.MentorSessions
                    .Where(ms => ms.StudentId == student.StudentId && ms.Status == "Completed" && ms.ReviewNotes != null)
                    .OrderByDescending(ms => ms.ScheduledAt)
                    .Select(ms => new StudentFeedbackDto
                    {
                        SessionId = ms.SessionId,
                        MentorName = ms.Mentor != null && ms.Mentor.FullName != null
                                     ? ms.Mentor.FullName : "Chuyên gia ẩn danh",
                        MentorCompany = ms.Mentor != null && ms.Mentor.CurrentCompany != null
                                     ? ms.Mentor.CurrentCompany : "Tech Industry",
                        ReviewNotes = ms.ReviewNotes,
                        ScheduledAt = ms.ScheduledAt
                    })
                    .ToListAsync();

                return (200, "Lấy danh sách nhận xét thành công.", feedbacks);
            }
            catch (Exception ex)
            {
                return (500, $"Lỗi hệ thống: {ex.Message}", null);
            }
        }

        // ===============================================
        // BỔ SUNG: HÀM TÌM CỐ VẤN CHO SINH VIÊN
        // ===============================================
        public async Task<object?> GetMyCounselorAsync(Guid studentUserId)
        {
            // 1. Kiểm tra sinh viên có tồn tại không
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == studentUserId);
            if (student == null) return null;

            // 2. Tìm Counselor. (Tạm thời lấy Counselor đầu tiên trong DB)
            var counselor = await _context.Counselors.FirstOrDefaultAsync();
            if (counselor == null) return null;

            // 3. Trả về object ẩn danh
            return new
            {
                counselorId = counselor.CounselorId,
                fullName = counselor.FullName ?? "Cố vấn hệ thống",
                department = counselor.Department ?? "Phòng ban chung"
            };
        }
    }
}