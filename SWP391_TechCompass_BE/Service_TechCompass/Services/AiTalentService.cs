using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Repository_TechCompass.Interfaces;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class AiTalentService : IAiTalentService
    {
        private readonly IStudentRepository _studentRepository;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public AiTalentService(IStudentRepository studentRepository, HttpClient httpClient, IConfiguration configuration)
        {
            _studentRepository = studentRepository;
            _httpClient = httpClient;
            _configuration = configuration;
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
                // 1. Chuẩn bị Prompt
                string patterns = string.Join("\n- ", codingPatterns);
                string prompt = $"Dựa vào các lịch sử làm bài và pattern code sau của sinh viên phần mềm, hãy phân tích ngắn gọn (khoảng 3-4 câu) về tài năng tiềm ẩn, tư duy logic và định hướng vai trò phù hợp nhất (VD: System Design, Backend, UI/UX, DevOps...):\n- {patterns}";

                // 2. Gọi Gemini API
                aiGeneratedTalent = await CallGeminiApiAsync(prompt);
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

        // --- HÀM HỖ TRỢ GỌI GEMINI API ---
        private async Task<string> CallGeminiApiAsync(string prompt)
        {
            try
            {
                string baseUrl = _configuration["GeminiApiConfig:BaseUrl"]
                    ?? throw new Exception("Thiếu cấu hình BaseUrl của Gemini");
                string apiKey = _configuration["GeminiApiConfig:ApiKey"]
                    ?? throw new Exception("Thiếu cấu hình ApiKey của Gemini");

                string requestUrl = $"{baseUrl}?key={apiKey}";

                // Build Payload theo chuẩn API của Gemini
                var payload = new
                {
                    contents = new[]
                    {
                        new { parts = new[] { new { text = prompt } } }
                    }
                };

                var response = await _httpClient.PostAsJsonAsync(requestUrl, payload);
                response.EnsureSuccessStatusCode(); // Ném lỗi nếu status code không phải 2xx

                var jsonResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>();

                // Trích xuất text từ JSON response của Gemini
                string resultText = jsonResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                return string.IsNullOrWhiteSpace(resultText)
                    ? "Không thể phân tích dữ liệu lúc này, vui lòng thử lại sau."
                    : resultText;
            }
            catch (Exception ex)
            {
                // Ghi log lỗi ở đây nếu có ILogger
                return $"Lỗi khi kết nối với AI Engine: {ex.Message}";
            }
        }
    }

    // --- CÁC CLASS ĐỂ PARSE KẾT QUẢ TỪ GEMINI ---
    public class GeminiResponse
    {
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    public class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
    }

    public class GeminiContent
    {
        public List<GeminiPart>? Parts { get; set; }
    }

    public class GeminiPart
    {
        public string? Text { get; set; }
    }
}