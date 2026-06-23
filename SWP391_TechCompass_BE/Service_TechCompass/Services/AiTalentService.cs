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
        private readonly IChatCompletionService _codeAnalyzerService;

        public AiTalentService(IStudentRepository studentRepository, Kernel kernel)
        {
            _studentRepository = studentRepository;
            _codeAnalyzerService = kernel.GetRequiredService<IChatCompletionService>("GeminiChat");
        }

        public async Task<TalentAnalysisDto> GenerateLatentTalentAsync(Guid studentId)
        {
            var student = await _studentRepository.GetStudentWithAssessmentsAsync(studentId);
            if (student == null) throw new Exception("Không tìm thấy sinh viên.");

            string existingSummary = student.LatentTalentSummary ?? "Chưa có đánh giá ban đầu.";

            var codingPatterns = student.SkillAssessments
                                        .Select(a => a.CodingPatternSnapshot)
                                        .Where(p => !string.IsNullOrEmpty(p))
                                        .ToList();

            string aiGeneratedTalent;

            if (!codingPatterns.Any())
            {
                return new TalentAnalysisDto
                {
                    StudentId = student.StudentId,
                    LatentTalentSummary = existingSummary
                };
            }

            string patterns = string.Join("\n- ", codingPatterns);

            var chatHistory = new ChatHistory();

            chatHistory.AddSystemMessage(@"Bạn là một Senior Software Architect kiêm Mentor hướng nghiệp. Nhiệm vụ của bạn là đánh giá sự tiến bộ của sinh viên IT. 
Bạn sẽ nhận được 'Đánh giá quá khứ' và 'Lịch sử code thực tế' gần đây. 
Hãy tổng hợp, so sánh và đưa ra một ĐÁNH GIÁ CẬP NHẬT ngắn gọn (3-5 câu), chỉ ra sự tiến bộ, điểm mạnh cốt lõi và điều chỉnh định hướng nghề nghiệp nếu cần. 
Trực diện, chuyên nghiệp, KHÔNG dùng markdown định dạng phức tạp.");

            string prompt = $@"
[Đánh giá quá khứ]:
{existingSummary}

[Lịch sử code thực tế gần đây]:
- {patterns}

Dựa trên dữ liệu trên, hãy viết lại bản tóm tắt năng lực tiềm ẩn (Latent Talent Summary) phiên bản MỚI NHẤT. Đừng lặp lại nguyên văn đánh giá cũ, hãy viết tiếp câu chuyện phát triển của sinh viên.";

            chatHistory.AddUserMessage(prompt);

            try
            {
                var response = await _codeAnalyzerService.GetChatMessageContentAsync(chatHistory);
                aiGeneratedTalent = response.ToString() ?? "";

                if (string.IsNullOrWhiteSpace(aiGeneratedTalent))
                {
                    throw new Exception("AI trả về kết quả rỗng.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LỖI AI TALENT EVOLUTION]: {ex.Message}");
                return new TalentAnalysisDto
                {
                    StudentId = student.StudentId,
                    LatentTalentSummary = existingSummary
                };
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
                LatentTalentSummary = student.LatentTalentSummary ?? "Hệ thống đang chờ thêm dữ liệu để phân tích năng lực của bạn."
            };
        }

        public async Task GenerateLatentTalentForAllStudentsAsync()
        {
            var allStudents = await _studentRepository.GetAllStudentsAsync();
            foreach (var student in allStudents)
            {
                try
                {
                    await GenerateLatentTalentAsync(student.StudentId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[JOB ERROR] Lỗi phân tích tự động cho student {student.StudentId}: {ex.Message}");
                }
            }
        }
    }
}