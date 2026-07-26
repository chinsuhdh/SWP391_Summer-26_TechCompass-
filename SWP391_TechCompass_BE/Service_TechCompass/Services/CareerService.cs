using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Repository_TechCompass.Interfaces;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class CareerService : ICareerService
    {
        private readonly IUserRepository _userRepo;
        private readonly IChatCompletionService _chatCompletionService;

        public CareerService(IUserRepository userRepo, Kernel kernel)
        {
            _userRepo = userRepo;
            // Trích xuất service chat Gemini từ Semantic Kernel DI Container
            _chatCompletionService = kernel.GetRequiredService<IChatCompletionService>("GeminiChat");
        }

        // 1. Chức năng: Khảo sát định hướng nghề nghiệp (Sử dụng Semantic Kernel với Gemini)
        public async Task<(int StatusCode, string Message, string? AnalyzedResult)> SubmitSurveyAsync(Guid userId, SubmitSurveyDto request)
        {
            var student = _userRepo.GetStudentByUserId(userId);
            if (student == null) return (404, "Không tìm thấy hồ sơ sinh viên.", null);

            // Thiết kế Prompt cho AI Mentor
            string prompt = $@"
Bạn là một chuyên gia định hướng nghề nghiệp IT (Senior Career Mentor). 
Hãy đọc kết quả khảo sát của sinh viên này:
- Ngôn ngữ lập trình yêu thích: {request.ProgrammingLanguagePreference}
- Kỹ năng giải quyết vấn đề: {request.ProblemSolvingSkill}
- Môi trường làm việc mong muốn: {request.WorkEnvironmentPreference}
- Mục tiêu nghề nghiệp: {request.CareerGoal}

Nhiệm vụ: Viết một đoạn phân tích ngắn gọn, trực diện (tối đa 4 câu) bằng tiếng Việt để đánh giá tài năng tiềm ẩn (Latent Talent) của sinh viên này. Gợi ý rõ ràng họ phù hợp nhất với vai trò nào (VD: Backend, Frontend, DevOps, Data Engineer, v.v.). Đừng giải thích dài dòng, hãy nói giọng văn chuyên nghiệp, truyền cảm hứng.
";

            string aiAnalysis = string.Empty;

            try
            {
                // Gọi API thông qua Semantic Kernel
                var chatHistory = new ChatHistory();
                chatHistory.AddSystemMessage("Bạn là Senior Career Mentor đóng vai trò tư vấn định hướng công nghệ súc tích.");
                chatHistory.AddUserMessage(prompt);

                var response = await _chatCompletionService.GetChatMessageContentAsync(chatHistory);
                aiAnalysis = response.ToString() ?? "Hệ thống AI không thể phân tích dữ liệu lúc này.";
            }
            catch (Exception ex)
            {
                // Fallback an toàn khi Gemini API gặp sự cố hoặc hết Quota
                Console.WriteLine($"[LỖI GEMINI SURVEY ANALYZER]: {ex.Message}");
                aiAnalysis = "Hệ thống AI hiện đang bảo trì hoặc quá tải. Dữ liệu khảo sát của bạn đã được ghi nhận và sẽ được phân tích sau.";
            }

            // Lưu kết quả đánh giá của AI vào Database
            student.LatentTalentSummary = aiAnalysis.Trim();
            student.UpdatedAt = DateTime.Now;

            _userRepo.UpdateStudent(student);
            _userRepo.SaveChanges();

            return (200, "Đã nộp khảo sát thành công. AI đã hoàn tất phân tích tiềm năng của bạn.", student.LatentTalentSummary);
        }

        // 2. Chức năng: Chọn nghề nghiệp mục tiêu
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