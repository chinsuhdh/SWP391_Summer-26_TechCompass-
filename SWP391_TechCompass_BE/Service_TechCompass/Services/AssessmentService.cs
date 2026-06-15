using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs;
using Service_TechCompass.DTOs.Assessment;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class AssessmentService : IAssessmentService
    {
        private readonly IAssessmentRepository _repository;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public AssessmentService(IAssessmentRepository repository, HttpClient httpClient, IConfiguration configuration)
        {
            _repository = repository;
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<List<QuizQuestionDto>> GetQuizBySkillNodeAsync(int skillNodeId)
        {
            var realQuestions = await _repository.GetQuestionsBySkillNodeAsync(skillNodeId, 10);
            var result = new List<QuizQuestionDto>();

            foreach (var q in realQuestions)
            {
                var options = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(q.OptionA)) options.Add("A", q.OptionA);
                if (!string.IsNullOrEmpty(q.OptionB)) options.Add("B", q.OptionB);
                if (!string.IsNullOrEmpty(q.OptionC)) options.Add("C", q.OptionC);
                if (!string.IsNullOrEmpty(q.OptionD)) options.Add("D", q.OptionD);

                result.Add(new QuizQuestionDto
                {
                    QuestionId = q.QuestionId,
                    QuestionText = q.QuestionText,
                    Options = options
                });
            }
            return result;
        }

        public async Task<SkillAssessment> GradeAndSaveQuizAsync(QuizSubmissionDto submission)
        {
            var questionIds = submission.Answers.Select(a => a.QuestionId).ToList();
            var dbQuestions = await _repository.GetQuestionsByIdsAsync(questionIds);
            int correctCount = 0;

            foreach (var answer in submission.Answers)
            {
                var matchedQuestion = dbQuestions.FirstOrDefault(q => q.QuestionId == answer.QuestionId);
                if (matchedQuestion != null && matchedQuestion.CorrectAnswer.Equals(answer.SelectedOption, StringComparison.OrdinalIgnoreCase))
                {
                    correctCount++;
                }
            }

            decimal finalScore = dbQuestions.Count > 0 ? (decimal)correctCount / dbQuestions.Count * 10 : 0;
            string feedback = finalScore >= 8 ? "Xuất sắc! Bạn đã đủ điều kiện pass node kỹ năng này." :
                              finalScore >= 5 ? "Khá tốt, nhưng bạn cần nắm vững lý thuyết hơn trước khi thực hành." :
                                                "Bạn bị hổng kiến thức nghiêm trọng. Hãy xem lại các Learning Resource.";

            var assessmentRecord = new SkillAssessment
            {
                AssessmentId = Guid.NewGuid(),
                StudentId = submission.StudentId,
                SkillNodeId = submission.SkillNodeId,
                TestScore = finalScore,
                TakenAt = DateTime.Now,
                AiFeedback = feedback,
                CodingPatternSnapshot = "Real_DB_Quiz_Logic"
            };
            return await _repository.SaveAssessmentResultAsync(assessmentRecord);
        }

        public async Task<SkillAssessment> GradeAndSaveCodeTestAsync(CodeTestSubmissionDto submission)
        {
            decimal executionScore = 0.0m;
            string codeExecutionFeedback = string.Empty;

            // ==========================================
            // BƯỚC 1: CHẤM CODE BẰNG JDOODLE
            // ==========================================
            string clientId = _configuration["JDoodleConfig:ClientId"];
            string clientSecret = _configuration["JDoodleConfig:ClientSecret"];
            string apiUrl = "https://api.jdoodle.com/v1/execute";

            string jLanguage = submission.Language.ToLower() switch
            {
                "csharp" => "csharp",
                "javascript" => "nodejs",
                "python" => "python3",
                "java" => "java",
                "c" => "c",
                "cpp" => "cpp14",
                _ => "csharp"
            };
            string jVersion = jLanguage == "csharp" ? "4" : "0";

            var jdoodleReq = new
            {
                clientId = clientId,
                clientSecret = clientSecret,
                script = submission.SourceCode,
                language = jLanguage,
                versionIndex = jVersion,
                stdin = submission.Stdin
            };

            try
            {
                var jResponse = await _httpClient.PostAsJsonAsync(apiUrl, jdoodleReq);
                var resultString = await jResponse.Content.ReadAsStringAsync();

                if (jResponse.IsSuccessStatusCode)
                {
                    using var jsonDoc = JsonDocument.Parse(resultString);
                    var root = jsonDoc.RootElement;

                    if (root.TryGetProperty("error", out var errorEl) && !string.IsNullOrEmpty(errorEl.GetString()))
                    {
                        codeExecutionFeedback = "JDoodle Error: " + errorEl.GetString();
                    }
                    else
                    {
                        string actualOutput = root.TryGetProperty("output", out var outputEl) ? outputEl.GetString().Trim() : "";
                        string expectedOutput = submission.ExpectedOutput?.Trim() ?? "";

                        // Chuẩn hóa chuỗi (tránh lỗi khác biệt \r\n giữa Windows và Linux)
                        string normalizedActual = actualOutput.Replace("\r\n", "\n");
                        string normalizedExpected = expectedOutput.Replace("\r\n", "\n");

                        // Chấm điểm: Nếu Output rỗng (bài tập tự do) hoặc Output thực tế chứa Output mong đợi
                        if (string.IsNullOrEmpty(normalizedExpected) || normalizedActual.Contains(normalizedExpected))
                        {
                            executionScore = 10.0m;
                            codeExecutionFeedback = "Code chạy hoàn hảo, Pass toàn bộ Test Cases.";
                        }
                        else
                        {
                            executionScore = 0.0m;
                            codeExecutionFeedback = $"Sai kết quả đầu ra.\n- Mong đợi: {expectedOutput}\n- Thực tế: {actualOutput}";
                        }
                    }
                }
                else
                {
                    codeExecutionFeedback = "Lỗi HTTP từ JDoodle: " + resultString;
                }
            }
            catch (Exception ex)
            {
                codeExecutionFeedback = $"Lỗi hệ thống khi kết nối Compiler: {ex.Message}";
            }

            // ==========================================
            // BƯỚC 2: GỌI GEMINI AI ĐỂ REVIEW CODE
            // ==========================================
            string gApiKey = _configuration["GeminiApiConfig:ApiKey"];
            string gBaseUrl = _configuration["GeminiApiConfig:BaseUrl"];
            string gRequestUrl = $"{gBaseUrl}?key={gApiKey}";

            string prompt = $@"Bạn là một Senior Software Engineer đóng vai trò Code Reviewer.
Đề bài: {submission.ProblemDescription}
Ngôn ngữ: {submission.Language}
Kết quả biên dịch: {codeExecutionFeedback}
Điểm hệ thống tự động chấm: {executionScore}/10
Mã nguồn sinh viên: {submission.SourceCode}
Hãy phân tích ngắn gọn dưới 150 chữ:
1. Độ phức tạp thuật toán (Big O)
2. Clean Code
3. Latent Talent (Tư duy thuật toán)
Chỉ trả về nội dung nhận xét.";

            var geminiReq = new GeminiRequestDto
            {
                Contents = new List<GeminiContentDto> { new GeminiContentDto { Parts = new List<GeminiPartDto> { new GeminiPartDto { Text = prompt } } } }
            };

            var jsonPayload = JsonSerializer.Serialize(geminiReq);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            string aiFeedback = "Không thể kết nối đến AI Engine.";

            try
            {
                var gResponse = await _httpClient.PostAsync(gRequestUrl, content);
                if (gResponse.IsSuccessStatusCode)
                {
                    var responseData = await gResponse.Content.ReadAsStringAsync();
                    var geminiResponse = JsonSerializer.Deserialize<GeminiResponseDto>(responseData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    aiFeedback = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? aiFeedback;
                }
            }
            catch
            {
                // Bỏ qua lỗi AI, vẫn lưu kết quả chấm code
            }

            // ==========================================
            // BƯỚC 3: LƯU DATABASE VÀ TRẢ KẾT QUẢ
            // ==========================================
            var assessmentRecord = new SkillAssessment
            {
                AssessmentId = Guid.NewGuid(),
                StudentId = submission.StudentId,
                SkillNodeId = submission.SkillNodeId,
                TestScore = executionScore,
                TakenAt = DateTime.Now,
                AiFeedback = aiFeedback.Trim(),
                CodingPatternSnapshot = submission.SourceCode
            };

            return await _repository.SaveAssessmentResultAsync(assessmentRecord);
        }

        public async Task<AssessmentFeedbackDto> GetAssessmentFeedbackAsync(Guid assessmentId)
        {
            var assessment = await _repository.GetAssessmentByIdAsync(assessmentId);
            if (assessment == null) throw new Exception("Không tìm thấy kết quả bài kiểm tra này.");

            return new AssessmentFeedbackDto
            {
                AssessmentId = assessment.AssessmentId,
                NodeName = assessment.SkillNode?.NodeName,
                TestScore = assessment.TestScore,
                AiFeedback = assessment.AiFeedback,
                TakenAt = assessment.TakenAt
            };
        }

        public async Task<IEnumerable<object>> GetAllSkillNodesAsync()
        {
            var nodes = await _repository.GetAllSkillNodesAsync();

            return nodes.Select(n => new
            {
                id = n.SkillNodeId,
                name = n.NodeName
            });
        }

        // 1. Tạo class hứng cấu trúc JSON do AI trả về
        private class GeminiExerciseDto
        {
            public string Title { get; set; } = string.Empty;
            public string ProblemDescription { get; set; } = string.Empty;
            public string DefaultCodeTemplate { get; set; } = string.Empty;
            public string TestStdin { get; set; } = string.Empty;
            public string ExpectedOutput { get; set; } = string.Empty;
            public string DifficultyLevel { get; set; } = string.Empty;
        }

        // 2. Viết hàm sinh đề
        public async Task<CodingExercise> GetOrGenerateCodingExerciseAsync(int skillNodeId)
        {
            // Bước 1: Kiểm tra xem Database đã có bài tập cho kỹ năng này chưa
            var existingExercise = await _repository.GetCodingExerciseByNodeAsync(skillNodeId);
            if (existingExercise != null)
            {
                // Phải ngắt kết nối vòng lặp trước khi return
                existingExercise.SkillNode = null!;
                return existingExercise;
            }

            // Bước 2: Nếu chưa có, lấy tên Kỹ năng để yêu cầu AI ra đề
            var node = await _repository.GetSkillNodeByIdAsync(skillNodeId);
            if (node == null) throw new Exception("Không tìm thấy kỹ năng này trong Database.");

            // Bước 3: THIẾT LẬP PROMPT THẦN THÁNH BẮT AI TRẢ VỀ JSON CHUẨN
            string prompt = $@"Bạn là một chuyên gia tạo đề thi lập trình (Problem Setter) trên HackerRank.
Hãy tạo 1 bài tập lập trình cơ bản bằng ngôn ngữ C# để kiểm tra kỹ năng '{node.NodeName}'.
YÊU CẦU BẮT BUỘC: CHỈ trả về ĐÚNG MỘT chuỗi JSON hợp lệ, KHÔNG thêm bất kỳ lời chào hay giải thích nào, KHÔNG dùng cú pháp markdown (như ```json).
Cấu trúc JSON bắt buộc phải giống hệt như sau:
{{
    ""Title"": ""Tên bài tập ngắn gọn"",
    ""ProblemDescription"": ""Mô tả yêu cầu bài toán chi tiết, rõ ràng."",
    ""DefaultCodeTemplate"": ""using System;\n\npublic class Solution {{\n    public static void Main() {{\n        // Viết code của bạn tại đây\n    }}\n}}"",
    ""TestStdin"": ""Dữ liệu đầu vào giả lập nhập từ Console (VD: 5\n10). Nếu bài không yêu cầu nhập, hãy để rỗng."",
    ""ExpectedOutput"": ""Kết quả in ra màn hình Console mong đợi để máy chấm tự động so sánh (VD: 15)."",
    ""DifficultyLevel"": ""Easy""
}}";

            string gApiKey = _configuration["GeminiApiConfig:ApiKey"] ?? throw new Exception("Thiếu API Key Gemini");
            string gBaseUrl = _configuration["GeminiApiConfig:BaseUrl"] ?? throw new Exception("Thiếu BaseUrl Gemini");
            string gRequestUrl = $"{gBaseUrl}?key={gApiKey}";

            var geminiReq = new GeminiRequestDto
            {
                Contents = new List<GeminiContentDto> { new GeminiContentDto { Parts = new List<GeminiPartDto> { new GeminiPartDto { Text = prompt } } } }
            };

            var content = new StringContent(JsonSerializer.Serialize(geminiReq), Encoding.UTF8, "application/json");

            // Bước 4: Gọi Gemini
            var gResponse = await _httpClient.PostAsync(gRequestUrl, content);
            if (!gResponse.IsSuccessStatusCode) throw new Exception("Lỗi gọi Gemini API để sinh đề.");

            var responseData = await gResponse.Content.ReadAsStringAsync();
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponseDto>(responseData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            string aiRawText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "";

            // Bước 5: Làm sạch chuỗi JSON (Đề phòng AI vẫn lén lút chèn markdown)
            aiRawText = aiRawText.Replace("```json", "").Replace("```", "").Trim();

            // Bước 6: Ép kiểu JSON thành Object
            var exerciseData = JsonSerializer.Deserialize<GeminiExerciseDto>(aiRawText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (exerciseData == null) throw new Exception("Gemini trả về sai định dạng dữ liệu.");

            // Bước 7: Lưu vào Database
            var newExercise = new CodingExercise
            {
                SkillNodeId = skillNodeId,
                Title = exerciseData.Title,
                ProblemDescription = exerciseData.ProblemDescription,
                DefaultCodeTemplate = exerciseData.DefaultCodeTemplate,
                TestStdin = exerciseData.TestStdin,
                ExpectedOutput = exerciseData.ExpectedOutput,
                DifficultyLevel = exerciseData.DifficultyLevel
            };

            var savedExercise = await _repository.SaveCodingExerciseAsync(newExercise);

            // TRỌNG TÂM LÀ Ở ĐÂY: Xóa sạch thông tin liên kết (SkillNode) trước khi trả về
            // Để quá trình biến thành JSON (Serialize) không bị dính vòng lặp!
            savedExercise.SkillNode = null!;

            return savedExercise;
        }

        public async Task<object?> GetMyLatestNodeResultAsync(Guid studentId, int skillNodeId)
        {
            var assessment = await _repository.GetLatestAssessmentByNodeAsync(studentId, skillNodeId);

            if (assessment == null) return null;

            return new
            {
                assessmentId = assessment.AssessmentId,
                skillNodeId = assessment.SkillNodeId,
                nodeName = assessment.SkillNode?.NodeName,
                testScore = assessment.TestScore,
                aiFeedback = assessment.AiFeedback,
                submittedCode = assessment.CodingPatternSnapshot, // Lấy lại code sinh viên đã viết
                takenAt = assessment.TakenAt
            };
        }

        public async Task<IEnumerable<object>> GetMyAssessmentHistoryListAsync(Guid studentId)
        {
            var history = await _repository.GetAssessmentsByStudentAsync(studentId);

            return history.Select(h => new
            {
                assessmentId = h.AssessmentId,
                skillNodeId = h.SkillNodeId,
                nodeName = h.SkillNode?.NodeName ?? "Kỹ năng ẩn",
                testScore = h.TestScore,
                takenAt = h.TakenAt,
                // ĐÃ THÊM: Kiểm tra xem đây là Quiz hay Code để báo cho Frontend
                isQuiz = h.CodingPatternSnapshot == "Real_DB_Quiz_Logic"
            });
        }

        public async Task<object?> GetAssessmentDetailByIdAsync(Guid assessmentId)
        {
            // Lấy đích danh bài thi thông qua ID duy nhất của nó
            var assessment = await _repository.GetAssessmentByIdAsync(assessmentId);
            if (assessment == null) return null;

            return new
            {
                assessmentId = assessment.AssessmentId,
                skillNodeId = assessment.SkillNodeId,
                nodeName = assessment.SkillNode?.NodeName ?? "Kỹ năng",
                testScore = assessment.TestScore,
                aiFeedback = assessment.AiFeedback,
                submittedCode = assessment.CodingPatternSnapshot,
                takenAt = assessment.TakenAt
            };
        }
    }
}