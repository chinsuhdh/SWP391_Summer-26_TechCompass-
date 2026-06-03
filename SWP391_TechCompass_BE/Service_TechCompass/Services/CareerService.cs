using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Repository_TechCompass.Interfaces;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class CareerService : ICareerService
    {
        private readonly IUserRepository _userRepo;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        // Bổ sung HttpClient và IConfiguration vào Constructor
        public CareerService(IUserRepository userRepo, HttpClient httpClient, IConfiguration configuration)
        {
            _userRepo = userRepo;
            _httpClient = httpClient;
            _configuration = configuration;
        }

        // 1. Chức năng: Khảo sát định hướng nghề nghiệp (Sử dụng Gemini 2.5 Flash)
        public async Task<(int StatusCode, string Message, string? AnalyzedResult)> SubmitSurveyAsync(Guid userId, SubmitSurveyDto request)
        {
            var student = _userRepo.GetStudentByUserId(userId);
            if (student == null) return (404, "Không tìm thấy hồ sơ sinh viên.", null);

            // 1. Thiết kế Prompt (Lời nhắc) cho AI
            string prompt = $@"
Bạn là một chuyên gia định hướng nghề nghiệp IT (Senior Career Mentor). 
Hãy đọc kết quả khảo sát của sinh viên này:
- Ngôn ngữ lập trình yêu thích: {request.ProgrammingLanguagePreference}
- Kỹ năng giải quyết vấn đề: {request.ProblemSolvingSkill}
- Môi trường làm việc mong muốn: {request.WorkEnvironmentPreference}
- Mục tiêu nghề nghiệp: {request.CareerGoal}

Nhiệm vụ: Viết một đoạn phân tích ngắn gọn, trực diện (tối đa 4 câu) bằng tiếng Việt để đánh giá tài năng tiềm ẩn (Latent Talent) của sinh viên này. Gợi ý rõ ràng họ phù hợp nhất với vai trò nào (VD: Backend, Frontend, DevOps, Data Engineer, v.v.). Đừng giải thích dài dòng, hãy nói giọng văn chuyên nghiệp, truyền cảm hứng.
";

            // 2. Chuẩn bị request gọi Gemini API
            string apiKey = _configuration["GeminiApiConfig:ApiKey"];
            string baseUrl = _configuration["GeminiApiConfig:BaseUrl"];
            string requestUrl = $"{baseUrl}?key={apiKey}"; // Gemini yêu cầu truyền key qua URL parameter

            var geminiReq = new GeminiRequestDto
            {
                Contents = new List<GeminiContentDto>
                {
                    new GeminiContentDto
                    {
                        Parts = new List<GeminiPartDto> { new GeminiPartDto { Text = prompt } }
                    }
                }
            };

            var jsonPayload = JsonSerializer.Serialize(geminiReq);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // 3. Gửi Request lên Google Server
            var response = await _httpClient.PostAsync(requestUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini API Error: {errorBody}");
            }

            // 4. Đọc và bóc tách kết quả
            var responseData = await response.Content.ReadAsStringAsync();
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponseDto>(responseData);

            string aiAnalysis = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text
                                ?? "Hệ thống AI không thể phân tích dữ liệu lúc này.";

            // 5. Lưu kết quả đánh giá thật của AI vào database
            student.LatentTalentSummary = aiAnalysis.Trim();
            student.UpdatedAt = DateTime.Now;

            _userRepo.UpdateStudent(student);
            _userRepo.SaveChanges();

            return (200, "Đã nộp khảo sát thành công. AI đã hoàn tất phân tích tiềm năng của bạn.", student.LatentTalentSummary);
        }

        // 2. Chức năng: Chọn nghề nghiệp mục tiêu (Giữ nguyên)
        public Task<(int StatusCode, string Message)> SelectTargetRoleAsync(Guid userId, SelectTargetRoleDto request)
        {
            var student = _userRepo.GetStudentByUserId(userId);
            if (student == null) return Task.FromResult<(int, string)>((404, "Không tìm thấy hồ sơ sinh viên."));

            student.TargetRoleId = request.TargetRoleId;
            student.UpdatedAt = DateTime.Now;

            _userRepo.UpdateStudent(student);
            _userRepo.SaveChanges();

            return Task.FromResult<(int, string)>((200, "Đã cập nhật mục tiêu nghề nghiệp thành công. Lộ trình học tập (Roadmap) sẽ được hệ thống sinh ra dựa trên lựa chọn này."));
        }
    }
}