using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs.Assessment;
using Service_TechCompass.Interfaces;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Service_TechCompass.Services
{
    public class AssessmentService : IAssessmentService
    {
        private readonly IAssessmentRepository _repository;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IQuizSyncService _quizSyncService;

        // Khai báo 2 biến để hứng 2 service khác nhau từ Kernel
        private readonly IChatCompletionService _geminiService;
        private readonly IChatCompletionService _openAiAnalyzer;

        public AssessmentService(
            IAssessmentRepository repository,
            HttpClient httpClient,
            IConfiguration configuration,
            Kernel kernel,
            IQuizSyncService quizSyncService)
        {
            _repository = repository;
            _httpClient = httpClient;
            _configuration = configuration;
            _quizSyncService = quizSyncService;

            // Kéo song song 2 Model từ Kernel DI thông qua ServiceId
            _geminiService = kernel.GetRequiredService<IChatCompletionService>("GeminiChat");
            _openAiAnalyzer = kernel.GetRequiredService<IChatCompletionService>("OpenAiCodeAnalyzer");
        }

        // ==========================================
        // 1. TẠO CÂU HỎI LÝ THUYẾT (TRẮC NGHIỆM) BẰNG AI
        // ==========================================
        public async Task<List<QuizQuestionDto>> GetQuizBySkillNodeAsync(int skillNodeId)
        {
            var realQuestions = await _repository.GetQuestionsBySkillNodeAsync(skillNodeId, 10);

            // Nếu DB chưa có câu hỏi nào (hoặc ít hơn 5 câu), dùng Gemini AI để tự động sinh đề mới
            if (realQuestions == null || realQuestions.Count < 5)
            {
                var node = await _repository.GetSkillNodeByIdAsync(skillNodeId);
                if (node != null)
                {
                    try
                    {
                        var chatHistory = new ChatHistory();
                        chatHistory.AddSystemMessage("You are a strict automated JSON array generator. Do not include markdown codeblocks like ```json.");

                        string prompt = $@"Bạn là một chuyên gia đào tạo IT cao cấp.
Hãy tạo 10 câu hỏi trắc nghiệm (Multiple Choice) bằng tiếng Việt để kiểm tra kỹ năng '{node.NodeName}'.
YÊU CẦU BẮT BUỘC: CHỈ trả về ĐÚNG MỘT mảng JSON hợp lệ, KHÔNG thêm bất kỳ lời chào hay giải thích nào.
Cấu trúc mảng JSON bắt buộc phải giống hệt như sau:
[
    {{
        ""QuestionText"": ""Nội dung câu hỏi lý thuyết sâu sắc về {node.NodeName}?"",
        ""OptionA"": ""Nội dung đáp án A"",
        ""OptionB"": ""Nội dung đáp án B"",
        ""OptionC"": ""Nội dung đáp án C"",
        ""OptionD"": ""Nội dung đáp án D"",
        ""CorrectAnswer"": ""A"", 
        ""Explanation"": ""Giải thích ngắn gọn tại sao đáp án này đúng."",
        ""DifficultyLevel"": ""Medium""
    }}
]";
                        chatHistory.AddUserMessage(prompt);

                        // DÙNG GEMINI ĐỂ SINH MẢNG JSON CÂU HỎI TRẮC NGHIỆM
                        var response = await _geminiService.GetChatMessageContentAsync(chatHistory);
                        string aiRawText = response.ToString() ?? throw new Exception("Lỗi gọi AI để sinh đề lý thuyết.");

                        int startIndex = aiRawText.IndexOf('[');
                        int endIndex = aiRawText.LastIndexOf(']');

                        if (startIndex >= 0 && endIndex >= startIndex)
                        {
                            aiRawText = aiRawText.Substring(startIndex, endIndex - startIndex + 1);
                        }
                        else
                        {
                            throw new Exception($"AI không trả về JSON Array hợp lệ. Data AI gửi về: {aiRawText}");
                        }

                        // Parse kết quả AI thành List các model DB
                        var generatedQuestions = JsonSerializer.Deserialize<List<AssessmentQuestion>>(
                            aiRawText,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        );

                        if (generatedQuestions != null && generatedQuestions.Count > 0)
                        {
                            foreach (var q in generatedQuestions)
                            {
                                q.SkillNodeId = skillNodeId;
                                // Chuẩn hóa CorrectAnswer (Đề phòng AI sinh thừa chữ như "A." thay vì "A")
                                q.CorrectAnswer = q.CorrectAnswer?.Trim().ToUpper();
                                if (q.CorrectAnswer?.Length > 1) q.CorrectAnswer = q.CorrectAnswer.Substring(0, 1);
                            }

                            // Lưu trực tiếp toàn bộ câu hỏi do AI tạo vào Database
                            await _repository.SaveQuestionsAsync(generatedQuestions);

                            // Load lại câu hỏi vừa lưu
                            realQuestions = await _repository.GetQuestionsBySkillNodeAsync(skillNodeId, 10);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Tự động tạo Quiz bằng AI thất bại]: {ex.Message}");
                        // Fallback an toàn: Nếu AI lỗi (do hết quota hoặc timeout), chạy lại API quizapi.io cũ
                        try
                        {
                            string keyword = node.NodeName.Split(' ')[0];
                            await _quizSyncService.FetchAndSaveQuestionsAsync(skillNodeId, keyword, 10);
                            realQuestions = await _repository.GetQuestionsBySkillNodeAsync(skillNodeId, 10);
                        }
                        catch { }
                    }
                }
            }

            var result = new List<QuizQuestionDto>();
            if (realQuestions != null)
            {
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
            }
            return result;
        }

        public async Task<IEnumerable<object>> GetAllSkillNodesAsync()
        {
            var nodes = await _repository.GetAllSkillNodesAsync();
            return nodes.Select(n => new { id = n.SkillNodeId, name = n.NodeName });
        }

        public async Task<CodingExercise> GetOrGenerateCodingExerciseAsync(int skillNodeId)
        {
            var existingExercise = await _repository.GetCodingExerciseByNodeAsync(skillNodeId);
            if (existingExercise != null)
            {
                existingExercise.SkillNode = null!;
                return existingExercise;
            }

            var node = await _repository.GetSkillNodeByIdAsync(skillNodeId);
            if (node == null) throw new Exception("Không tìm thấy kỹ năng này trong Database.");

            // LOGIC MỚI: PHÂN TÍCH NGÔN NGỮ LẬP TRÌNH DỰA THEO TÊN KỸ NĂNG (NODE NAME)
            string targetLanguage = "C#"; // Mặc định là C#
            string defaultTemplate = "using System;\\n\\npublic class Solution {\\n    public static void Main() {\\n        // Viết code của bạn tại đây\\n    }\\n}";

            string lowerNodeName = node.NodeName.ToLower();

            if (lowerNodeName.Contains("html") || lowerNodeName.Contains("css") || lowerNodeName.Contains("javascript") || lowerNodeName.Contains("dom"))
            {
                targetLanguage = "JavaScript (Node.js)";
                defaultTemplate = "// Viết mã JavaScript của bạn dưới đây để giải quyết bài toán\\n// Hàm console.log() sẽ in kết quả ra màn hình\\n\\nfunction solve() {\\n\\n}\\n\\nsolve();";
            }
            else if (lowerNodeName.Contains("react") || lowerNodeName.Contains("hooks"))
            {
                targetLanguage = "JavaScript (React Component Logic)";
                defaultTemplate = "// Viết mã logic JavaScript/React của bạn dưới đây\\n\\nconst App = () => {\\n    // ...\\n};";
            }
            else if (lowerNodeName.Contains("python") || lowerNodeName.Contains("data"))
            {
                targetLanguage = "Python";
                defaultTemplate = "def solve():\\n    # Viết mã Python của bạn tại đây\\n    pass\\n\\nif __name__ == '__main__':\\n    solve()";
            }
            else if (lowerNodeName.Contains("java") && !lowerNodeName.Contains("javascript"))
            {
                targetLanguage = "Java";
                defaultTemplate = "import java.util.*;\\n\\npublic class Solution {\\n    public static void main(String[] args) {\\n        // Viết mã Java của bạn tại đây\\n    }\\n}";
            }

            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage("You are a strict automated JSON generator. Do not include markdown codeblocks like ```json.");

            // CẬP NHẬT PROMPT ĐỂ TRUYỀN NGÔN NGỮ ĐÍCH VÀO
            string prompt = $@"Bạn là một chuyên gia tạo đề thi lập trình (Problem Setter) trên HackerRank.
Hãy tạo 1 bài tập lập trình cơ bản bằng ngôn ngữ '{targetLanguage}' để kiểm tra kỹ năng '{node.NodeName}'.
YÊU CẦU BẮT BUỘC: CHỈ trả về ĐÚNG MỘT chuỗi JSON hợp lệ, KHÔNG thêm bất kỳ lời chào hay giải thích nào.
Cấu trúc JSON bắt buộc phải giống hệt như sau:
{{
    ""Title"": ""Tên bài tập ngắn gọn"",
    ""ProblemDescription"": ""Mô tả yêu cầu bài toán chi tiết, rõ ràng."",
    ""DefaultCodeTemplate"": ""{defaultTemplate}"",
    ""TestStdin"": ""Dữ liệu đầu vào giả lập nhập từ Console. Nếu bài không yêu cầu nhập, hãy để rỗng."",
    ""ExpectedOutput"": ""Kết quả in ra màn hình Console mong đợi để máy chấm tự động so sánh."",
    ""DifficultyLevel"": ""Easy""
}}";
            chatHistory.AddUserMessage(prompt);

            try
            {
                // Gọi API Gemini
                var response = await _geminiService.GetChatMessageContentAsync(chatHistory);
                string aiRawText = response.ToString() ?? throw new Exception("Lỗi gọi AI để sinh đề bài.");

                int startIndex = aiRawText.IndexOf('{');
                int endIndex = aiRawText.LastIndexOf('}');

                if (startIndex >= 0 && endIndex >= startIndex)
                {
                    aiRawText = aiRawText.Substring(startIndex, endIndex - startIndex + 1);
                }
                else
                {
                    throw new Exception($"AI không trả về JSON hợp lệ. Data AI gửi về: {aiRawText}");
                }

                var exerciseData = JsonSerializer.Deserialize<GeminiExerciseDto>(aiRawText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (exerciseData == null) throw new Exception("AI trả về sai định dạng cấu trúc dữ liệu.");

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
                savedExercise.SkillNode = null!;

                return savedExercise;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi tạo đề AI: {ex.Message}");
            }
        }

        // ==========================================
        // 3. LUỒNG XỬ LÝ CHÍNH THEO SESSION
        // ==========================================
        public async Task<AssessmentSession> GradeAndSaveFullExamAsync(SubmitFullExamDto submission)
        {
            var session = new AssessmentSession
            {
                SessionId = Guid.NewGuid(),
                StudentId = submission.StudentId,
                SkillNodeId = submission.SkillNodeId,
                TakenAt = DateTime.Now
            };

            // 1. Chấm điểm trắc nghiệm và gán thực thể con
            var questionIds = submission.QuizAnswers.Select(a => a.QuestionId).ToList();
            var dbQuestions = await _repository.GetQuestionsByIdsAsync(questionIds);
            int correctCount = 0;

            foreach (var answer in submission.QuizAnswers)
            {
                var matchedQuestion = dbQuestions.FirstOrDefault(q => q.QuestionId == answer.QuestionId);
                bool isCorrect = matchedQuestion != null &&
                                 matchedQuestion.CorrectAnswer.Equals(answer.SelectedOption, StringComparison.OrdinalIgnoreCase);

                if (isCorrect) correctCount++;

                session.QuizDetails.Add(new AssessmentQuizDetail
                {
                    Id = Guid.NewGuid(),
                    SessionId = session.SessionId,
                    QuestionId = answer.QuestionId,
                    SelectedOption = answer.SelectedOption,
                    IsCorrect = isCorrect
                });
            }
            session.TotalQuizScore = dbQuestions.Count > 0 ? (decimal)correctCount / dbQuestions.Count * 10 : 0;

            // 2. Chấm điểm Code & Phân tích bằng OpenAI
            decimal executionScore = 0.0m;
            string codeExecutionFeedback = string.Empty;
            string aiFeedback = "Không thể kết nối đến AI Engine.";

            if (submission.CodeSubmission != null && !string.IsNullOrEmpty(submission.CodeSubmission.SourceCode))
            {
                string clientId = _configuration["JDoodleConfig:ClientId"];
                string clientSecret = _configuration["JDoodleConfig:ClientSecret"];
                string apiUrl = "https://api.jdoodle.com/v1/execute";

                string jLanguage = submission.CodeSubmission.Language.ToLower() switch
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
                    script = submission.CodeSubmission.SourceCode,
                    language = jLanguage,
                    versionIndex = jVersion,
                    stdin = submission.CodeSubmission.Stdin
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
                            string expectedOutput = submission.CodeSubmission.ExpectedOutput?.Trim() ?? "";

                            string normalizedActual = actualOutput.Replace("\r\n", "\n");
                            string normalizedExpected = expectedOutput.Replace("\r\n", "\n");

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
                }
                catch (Exception ex)
                {
                    codeExecutionFeedback = $"Lỗi Compiler: {ex.Message}";
                }

                // Thực hiện gọi Prompt phân tích Clean Code qua Semantic Kernel
                var chatHistory = new ChatHistory();
                chatHistory.AddSystemMessage("Bạn là một Senior Software Engineer đóng vai trò Code Reviewer. Hãy phân tích ngắn gọn, trực diện.");

                string prompt = $@"Đề bài: {submission.CodeSubmission.ProblemDescription}
Ngôn ngữ: {submission.CodeSubmission.Language}
Kết quả biên dịch: {codeExecutionFeedback}
Điểm hệ thống tự động chấm: {executionScore}/10
Mã nguồn sinh viên: {submission.CodeSubmission.SourceCode}
Hãy phân tích ngắn gọn dưới 150 chữ:
1. Độ phức tạp thuật toán (Big O)
2. Clean Code
3. Latent Talent (Tư duy thuật toán)
Chỉ trả về nội dung nhận xét.";

                chatHistory.AddUserMessage(prompt);

                try
                {
                    // ƯU TIÊN 1: DÙNG OPENAI ĐỂ ĐẢM BẢO KHẢ NĂNG ĐỌC HIỂU LOGIC VÀ THUẬT TOÁN
                    var response = await _openAiAnalyzer.GetChatMessageContentAsync(chatHistory);
                    aiFeedback = response.ToString();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n[LỖI OPENAI CODE REVIEW]: {ex.Message}");

                    try
                    {
                        Console.WriteLine("=> Đang chuyển hướng (Fallback) sang dùng Gemini để chấm bài...");
                        // ƯU TIÊN 2: NẾU OPENAI HẾT QUOTA / LỖI MẠNG -> CHUYỂN SANG GEMINI
                        var fallbackResponse = await _geminiService.GetChatMessageContentAsync(chatHistory);
                        aiFeedback = fallbackResponse.ToString();
                    }
                    catch (Exception geminiEx)
                    {
                        Console.WriteLine($"[LỖI GEMINI CODE REVIEW]: {geminiEx.Message}\n");
                        aiFeedback = $"Hệ thống AI đang bảo trì. Chi tiết lỗi OpenAI: {ex.Message}";
                    }
                }

                session.CodeDetail = new AssessmentCodeDetail
                {
                    Id = Guid.NewGuid(),
                    SessionId = session.SessionId,
                    SourceCode = submission.CodeSubmission.SourceCode,
                    AiFeedback = aiFeedback.Trim()
                };
            }
            session.TotalCodeScore = executionScore;

            return await _repository.SaveAssessmentSessionAsync(session);
        }

        public async Task<object?> GetMyLatestNodeResultAsync(Guid studentId, int skillNodeId)
        {
            var session = await _repository.GetLatestAssessmentSessionByNodeAsync(studentId, skillNodeId);
            if (session == null) return null;

            return new
            {
                assessmentId = session.SessionId,
                skillNodeId = session.SkillNodeId,
                nodeName = session.SkillNode?.NodeName,
                testScore = session.TotalQuizScore + session.TotalCodeScore,
                aiFeedback = session.CodeDetail?.AiFeedback,
                submittedCode = session.CodeDetail?.SourceCode,
                takenAt = session.TakenAt
            };
        }

        public async Task<IEnumerable<object>> GetMyAssessmentHistoryListAsync(Guid studentId)
        {
            var history = await _repository.GetAssessmentSessionsByStudentAsync(studentId);
            return history.Select(h => new
            {
                assessmentId = h.SessionId,
                sessionId = h.SessionId,
                skillNodeId = h.SkillNodeId,
                nodeName = h.SkillNode?.NodeName ?? "Kỹ năng ẩn",
                totalQuizScore = h.TotalQuizScore,
                totalCodeScore = h.TotalCodeScore,
                testScore = h.TotalQuizScore + h.TotalCodeScore,
                takenAt = h.TakenAt
            });
        }

        public async Task<object?> GetAssessmentDetailByIdAsync(Guid assessmentId)
        {
            var session = await _repository.GetAssessmentSessionByIdAsync(assessmentId);
            if (session == null) return null;

            return new
            {
                sessionId = session.SessionId,
                skillNodeId = session.SkillNodeId,
                nodeName = session.SkillNode?.NodeName ?? "Kỹ năng",
                totalQuizScore = session.TotalQuizScore,
                totalCodeScore = session.TotalCodeScore,
                takenAt = session.TakenAt,
                codeDetail = session.CodeDetail == null ? null : new
                {
                    sourceCode = session.CodeDetail.SourceCode,
                    aiFeedback = session.CodeDetail.AiFeedback
                },
                quizDetails = session.QuizDetails.Select(q => new
                {
                    questionId = q.QuestionId,
                    questionText = q.Question?.QuestionText,
                    selectedOption = q.SelectedOption,
                    correctAnswer = q.Question?.CorrectAnswer,
                    isCorrect = q.IsCorrect
                }).ToList()
            };
        }

        public async Task<SkillAssessment> GradeAndSaveQuizAsync(QuizSubmissionDto submission) => throw new NotImplementedException("Hàm cũ không sử dụng.");
        public async Task<SkillAssessment> GradeAndSaveCodeTestAsync(CodeTestSubmissionDto submission) => throw new NotImplementedException("Hàm cũ không sử dụng.");
        public async Task<AssessmentFeedbackDto> GetAssessmentFeedbackAsync(Guid assessmentId) => throw new NotImplementedException("Hàm cũ không sử dụng.");

        private class GeminiExerciseDto
        {
            public string Title { get; set; } = string.Empty;
            public string ProblemDescription { get; set; } = string.Empty;
            public string DefaultCodeTemplate { get; set; } = string.Empty;
            public string TestStdin { get; set; } = string.Empty;
            public string ExpectedOutput { get; set; } = string.Empty;
            public string DifficultyLevel { get; set; } = string.Empty;
        }

        // ==========================================
        // LUỒNG ĐÁNH GIÁ TOÀN DIỆN THEO NGHỀ NGHIỆP (ROLE-BASED)
        // ==========================================

        public async Task<List<QuizQuestionDto>> GetComprehensiveQuizByRoleAsync(int roleId)
        {
            var nodes = await _repository.GetSkillNodesByRoleIdAsync(roleId);
            if (nodes == null || !nodes.Any())
                throw new Exception("Chưa có kỹ năng nào được cấu hình cho ngành nghề này trong Database.");

            var finalQuestions = new List<AssessmentQuestion>();

            // Tính toán số lượng câu hỏi cần lấy cho mỗi kỹ năng để trải đều (Ví dụ: 10 câu / 5 kỹ năng = 2 câu/kỹ năng)
            int questionsPerNode = (int)Math.Ceiling(10.0 / nodes.Count);

            foreach (var node in nodes)
            {
                var dbQuestions = await _repository.GetQuestionsBySkillNodeAsync(node.SkillNodeId, 10);

                // NẾU KỸ NĂNG NÀY TRONG DB CHƯA CÓ ĐỦ CÂU HỎI -> GỌI AI TẠO VÀ LƯU XUỐNG DB
                if (dbQuestions == null || dbQuestions.Count < questionsPerNode)
                {
                    dbQuestions = await GenerateAndSaveQuestionsForNodeAsync(node.SkillNodeId, node.NodeName, 5);
                }

                if (dbQuestions != null && dbQuestions.Any())
                {
                    // Bốc ngẫu nhiên số lượng câu hỏi đã chia đều
                    finalQuestions.AddRange(dbQuestions.OrderBy(x => Guid.NewGuid()).Take(questionsPerNode));
                }

                if (finalQuestions.Count >= 10) break; // Dừng lại khi đã gom đủ 10 câu
            }

            // Shuffle toàn bộ đề thi lần cuối
            finalQuestions = finalQuestions.OrderBy(x => Guid.NewGuid()).Take(10).ToList();

            return finalQuestions.Select(q => new QuizQuestionDto
            {
                QuestionId = q.QuestionId,
                QuestionText = q.QuestionText,
                Options = new Dictionary<string, string>
        {
            { "A", q.OptionA ?? "True" },
            { "B", q.OptionB ?? "False" },
            { "C", q.OptionC ?? "" },
            { "D", q.OptionD ?? "" }
        }.Where(kv => !string.IsNullOrEmpty(kv.Value)).ToDictionary(kv => kv.Key, kv => kv.Value)
            }).ToList();
        }

        public async Task<CodingExercise> GetComprehensiveCodingExerciseByRoleAsync(int roleId)
        {
            var nodes = await _repository.GetSkillNodesByRoleIdAsync(roleId);
            if (nodes == null || !nodes.Any()) throw new Exception("Chưa có kỹ năng nào được cấu hình.");

            // Chọn node quan trọng nhất có yêu cầu Code (IsCodingRequired) để kiểm tra thực hành
            var targetNode = nodes.FirstOrDefault(n => n.IsCodingRequired == true) ?? nodes.First();

            return await GetOrGenerateCodingExerciseAsync(targetNode.SkillNodeId);
        }

        // HÀM HELPER: GỌI GEMINI AI TẠO CÂU HỎI VÀ LƯU VÀO DATABASE
        private async Task<List<AssessmentQuestion>> GenerateAndSaveQuestionsForNodeAsync(int skillNodeId, string nodeName, int count)
        {
            try
            {
                var chatHistory = new ChatHistory();
                chatHistory.AddSystemMessage("You are a strict automated JSON array generator. Do not include markdown codeblocks like ```json.");

                string prompt = $@"Bạn là một chuyên gia đào tạo IT cao cấp.
Hãy tạo {count} câu hỏi trắc nghiệm (Multiple Choice) bằng tiếng Việt để kiểm tra tư duy logic về kỹ năng '{nodeName}'.
YÊU CẦU BẮT BUỘC: CHỈ trả về ĐÚNG MỘT mảng JSON hợp lệ, KHÔNG thêm bất kỳ lời chào hay giải thích nào.
Cấu trúc JSON bắt buộc:
[
    {{
        ""QuestionText"": ""Nội dung câu hỏi sâu sắc về {nodeName}?"",
        ""OptionA"": ""Nội dung A"",
        ""OptionB"": ""Nội dung B"",
        ""OptionC"": ""Nội dung C"",
        ""OptionD"": ""Nội dung D"",
        ""CorrectAnswer"": ""A"", 
        ""Explanation"": ""Giải thích ngắn gọn."",
        ""DifficultyLevel"": ""Medium""
    }}
]";
                chatHistory.AddUserMessage(prompt);
                var response = await _geminiService.GetChatMessageContentAsync(chatHistory);
                string aiRawText = response.ToString();

                int startIndex = aiRawText.IndexOf('[');
                int endIndex = aiRawText.LastIndexOf(']');
                if (startIndex >= 0 && endIndex >= startIndex)
                {
                    aiRawText = aiRawText.Substring(startIndex, endIndex - startIndex + 1);
                }

                var generatedQuestions = JsonSerializer.Deserialize<List<AssessmentQuestion>>(
                    aiRawText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (generatedQuestions != null && generatedQuestions.Count > 0)
                {
                    foreach (var q in generatedQuestions)
                    {
                        q.SkillNodeId = skillNodeId;
                        q.CorrectAnswer = q.CorrectAnswer?.Trim().ToUpper();
                        if (q.CorrectAnswer?.Length > 1) q.CorrectAnswer = q.CorrectAnswer.Substring(0, 1);
                    }

                    await _repository.SaveQuestionsAsync(generatedQuestions);
                    return generatedQuestions;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Tự động tạo Quiz bằng AI thất bại cho Node {nodeName}]: {ex.Message}");
            }
            return new List<AssessmentQuestion>();
        }
    }
}