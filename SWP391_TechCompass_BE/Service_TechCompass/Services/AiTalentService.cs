using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Repository_TechCompass.Interfaces;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class AiTalentService : IAiTalentService
    {
        private readonly IStudentRepository _studentRepository;
        private readonly IChatCompletionService _chatCompletionService;

        // Bỏ HttpClient đi, thay bằng Kernel
        public AiTalentService(IStudentRepository studentRepository, Kernel kernel)
        {
            _studentRepository = studentRepository;
            // Kéo service chat Gemini mà chúng ta đã gán ID trong Program.cs ra
            _chatCompletionService = kernel.GetRequiredService<IChatCompletionService>("GeminiChat");
        }

        // Chức năng 27: AI Engine - Sinh latent talent
        public async Task<TalentAnalysisDto> GenerateLatentTalentAsync(Guid studentId)
        {
            var student = await _studentRepository.GetStudentWithAssessmentsAsync(studentId);
            if (student == null) throw new Exception("Không tìm thấy sinh viên.");

            var codingPatterns = student.SkillAssessments
                                        .Select(a => a.CodingPatternSnapshot)
                                        .Where(p => !string.IsNullOrEmpty(p))
                                        .ToList();

            string aiGeneratedTalent;

            if (!codingPatterns.Any())
            {
                aiGeneratedTalent = "Chưa có đủ dữ liệu từ các bài test để AI có thể phân tích tài năng tiềm ẩn.";
            }
            else
            {
                string patterns = string.Join("\n- ", codingPatterns);

                // Khởi tạo ChatHistory cho Semantic Kernel
                var chatHistory = new ChatHistory();

                // Set System Prompt để AI đóng vai trò chuyên gia đánh giá
                chatHistory.AddSystemMessage("Bạn là một chuyên gia đánh giá năng lực Software Engineering. Hãy phân tích ngắn gọn, trực diện, đi thẳng vào vấn đề kỹ thuật.");

                // User Prompt
                string prompt = $"Dựa vào các lịch sử làm bài và pattern code sau của sinh viên phần mềm, hãy phân tích ngắn gọn (khoảng 3-4 câu) về tài năng tiềm ẩn, tư duy logic và định hướng vai trò phù hợp nhất (VD: System Design, Backend, UI/UX, DevOps...):\n- {patterns}";
                chatHistory.AddUserMessage(prompt);

                try
                {
                    // Gọi Gemini qua Semantic Kernel
                    var response = await _chatCompletionService.GetChatMessageContentAsync(chatHistory);
                    aiGeneratedTalent = response.ToString() ?? "Không thể phân tích dữ liệu lúc này, vui lòng thử lại sau.";
                }
                catch (Exception ex)
                {
                    // Catch lỗi nếu API tạch hoặc hết quota
                    aiGeneratedTalent = $"Lỗi khi kết nối với AI Engine: {ex.Message}";
                }
            }

            // 3. Cập nhật vào Database
            student.LatentTalentSummary = aiGeneratedTalent;
            await _studentRepository.UpdateStudentAsync(student);

            return new TalentAnalysisDto
            {
                StudentId = student.StudentId,
                LatentTalentSummary = student.LatentTalentSummary
            };
        }

        // Chức năng 28: Sinh viên - Xem AI talent analysis
        public async Task<TalentAnalysisDto> GetTalentAnalysisAsync(Guid studentId)
        {
            var student = await _studentRepository.GetStudentByIdAsync(studentId);
            if (student == null) throw new Exception("Không tìm thấy sinh viên.");

            return new TalentAnalysisDto
            {
                StudentId = student.StudentId,
                LatentTalentSummary = student.LatentTalentSummary ?? "AI chưa phân tích xong dữ liệu của bạn."
            };
        }
    }
}