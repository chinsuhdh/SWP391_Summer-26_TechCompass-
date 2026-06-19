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
        private readonly IChatCompletionService _codeAnalyzerService; // Sử dụng OpenAI

        public AiTalentService(IStudentRepository studentRepository, Kernel kernel)
        {
            _studentRepository = studentRepository;
            // Gọi đúng ServiceId đã đăng ký trong Program.cs
            _codeAnalyzerService = kernel.GetRequiredService<IChatCompletionService>("OpenAiCodeAnalyzer");
        }

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

                var chatHistory = new ChatHistory();
                // System prompt cho OpenAI cần chi tiết và khắt khe hơn
                chatHistory.AddSystemMessage("Bạn là một Senior Software Architect. Nhiệm vụ của bạn là đọc các coding patterns này và xác định thiên hướng (Latent Talent) của sinh viên (VD: System Design, Database Optimization, UI/UX). Trả lời ngắn gọn, trực diện, đi thẳng vào vấn đề kỹ thuật.");

                string prompt = $"Dựa vào các lịch sử làm bài và pattern code sau của sinh viên phần mềm, hãy phân tích ngắn gọn (khoảng 3-4 câu) về tài năng tiềm ẩn, tư duy logic và định hướng vai trò phù hợp nhất:\n- {patterns}";
                chatHistory.AddUserMessage(prompt);

                try
                {
                    // Thực thi với GPT-4o-mini
                    var response = await _codeAnalyzerService.GetChatMessageContentAsync(chatHistory);
                    aiGeneratedTalent = response.ToString() ?? "Không thể phân tích dữ liệu lúc này, vui lòng thử lại sau.";
                }
                catch (Exception ex)
                {
                    aiGeneratedTalent = $"Lỗi khi kết nối với AI Engine: {ex.Message}";
                }
            }

            student.LatentTalentSummary = aiGeneratedTalent;
            await _studentRepository.UpdateStudentAsync(student);

            return new TalentAnalysisDto
            {
                StudentId = student.StudentId,
                LatentTalentSummary = student.LatentTalentSummary
            };
        }

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