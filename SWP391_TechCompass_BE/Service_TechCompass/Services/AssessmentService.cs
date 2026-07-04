using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Repository_TechCompass;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs.Assessment;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class AssessmentService : IAssessmentService
    {
        private readonly IAssessmentRepository _repository;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IQuizSyncService _quizSyncService;

        private readonly IChatCompletionService _geminiService;
        private readonly IChatCompletionService _openAiAnalyzer;

        private readonly Swp391CareerRoadmapContext _context;

        public AssessmentService(
            IAssessmentRepository repository,
            HttpClient httpClient,
            IConfiguration configuration,
            Kernel kernel,
            IQuizSyncService quizSyncService,
            Swp391CareerRoadmapContext context)
        {
            _repository = repository;
            _httpClient = httpClient;
            _configuration = configuration;
            _quizSyncService = quizSyncService;
            _context = context;

            _geminiService = kernel.GetRequiredService<IChatCompletionService>("GeminiChat");
            _openAiAnalyzer = kernel.GetRequiredService<IChatCompletionService>("OpenAiCodeAnalyzer");
        }

        // ==========================================
        // 1. TẠO CÂU HỎI LÝ THUYẾT (TRẮC NGHIỆM) BẰNG AI
        // ==========================================
        public async Task<List<QuizQuestionDto>> GetQuizBySkillNodeAsync(int skillNodeId)
        {
            var realQuestions = await _repository.GetQuestionsBySkillNodeAsync(skillNodeId, 10);

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

                        var response = await _geminiService.GetChatMessageContentAsync(chatHistory);
                        string aiRawText = response.ToString() ?? throw new Exception("Lỗi gọi AI để sinh đề lý thuyết.");

                        // XÓA MARKDOWN BLOCK NẾU CÓ
                        if (aiRawText.Contains("```"))
                        {
                            aiRawText = aiRawText.Replace("```json", "").Replace("```", "").Trim();
                        }

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

                        var generatedQuestions = JsonSerializer.Deserialize<List<AssessmentQuestion>>(
                            aiRawText,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
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
                            realQuestions = await _repository.GetQuestionsBySkillNodeAsync(skillNodeId, 10);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Tự động tạo Quiz bằng AI thất bại]: {ex.Message}");
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

            string targetLanguage = string.Empty;
            string defaultTemplate = string.Empty;
            string lowerNodeName = node.NodeName.ToLower();

            // TỐI ƯU THUẬT TOÁN ĐOÁN NGÔN NGỮ TỪ TÊN KỸ NĂNG
            if (lowerNodeName.Contains("sql") || lowerNodeName.Contains("database"))
            {
                targetLanguage = "SQL";
                defaultTemplate = "-- Viết câu lệnh SQL của bạn tại đây\nSELECT * FROM ...";
            }
            else if (lowerNodeName.Contains("linux") || lowerNodeName.Contains("shell") || lowerNodeName.Contains("bash"))
            {
                targetLanguage = "Bash";
                defaultTemplate = "#!/bin/bash\n# Viết script bash tại đây";
            }
            else if (lowerNodeName.Contains("html") || lowerNodeName.Contains("css") || lowerNodeName.Contains("javascript") || lowerNodeName.Contains("dom") || lowerNodeName.Contains("react") || lowerNodeName.Contains("hooks") || lowerNodeName.Contains("node"))
            {
                targetLanguage = "JavaScript (Node.js)";
                defaultTemplate = "// Viết mã JavaScript của bạn dưới đây\n// Hàm console.log() sẽ in kết quả ra màn hình\n\nfunction solve() {\n\n}\n\nsolve();";
            }
            else if (lowerNodeName.Contains("python") || lowerNodeName.Contains("data"))
            {
                targetLanguage = "Python";
                defaultTemplate = "def solve():\n    # Viết mã Python của bạn tại đây\n    pass\n\nif __name__ == '__main__':\n    solve()";
            }
            else if (lowerNodeName.Contains("java") && !lowerNodeName.Contains("javascript"))
            {
                targetLanguage = "Java";
                defaultTemplate = "import java.util.*;\n\npublic class Solution {\n    public static void main(String[] args) {\n        // Viết mã Java của bạn tại đây\n    }\n}";
            }
            else if (lowerNodeName.Contains("c++") || lowerNodeName.Contains("cpp"))
            {
                targetLanguage = "C++";
                defaultTemplate = "#include <iostream>\nusing namespace std;\n\nint main() {\n    // Viết code C++ của bạn tại đây\n    return 0;\n}";
            }
            else if (lowerNodeName.Contains("c#") || lowerNodeName.Contains("csharp") || lowerNodeName.Contains("net") || lowerNodeName.Contains("oop") || lowerNodeName.Contains("linq"))
            {
                targetLanguage = "C#";
                defaultTemplate = "using System;\n\npublic class Solution {\n    public static void Main() {\n        // Viết code của bạn tại đây\n    }\n}";
            }

            // NẾU TÊN NODE KHÔNG THUỘC NGÔN NGỮ NÀO -> BÁO LỖI (CHẶN GỌI AI)
            if (string.IsNullOrEmpty(targetLanguage))
            {
                throw new Exception($"Kỹ năng '{node.NodeName}' không hỗ trợ bài kiểm tra lập trình tự động bằng Code Editor.");
            }

            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage("You are a strict automated JSON generator. Do not include markdown codeblocks like ```json.");

            string prompt = $@"Bạn là một chuyên gia tạo đề thi lập trình (Problem Setter) trên HackerRank.
Hãy tạo 1 bài tập thực hành cơ bản bằng ngôn ngữ '{targetLanguage}' để kiểm tra kỹ năng '{node.NodeName}'.
YÊU CẦU BẮT BUỘC: CHỈ trả về ĐÚNG MỘT chuỗi JSON hợp lệ, KHÔNG thêm bất kỳ lời chào hay giải thích nào.
Cấu trúc JSON bắt buộc phải giống hệt như sau:
{{
    ""Title"": ""Tên bài tập ngắn gọn"",
    ""ProblemDescription"": ""Mô tả yêu cầu bài toán chi tiết, rõ ràng."",
    ""DefaultCodeTemplate"": ""{defaultTemplate.Replace("\"", "\\\"").Replace("\n", "\\n")}"",
    ""TestStdin"": ""Dữ liệu đầu vào giả lập nhập từ Console. Nếu bài không yêu cầu nhập, hãy để rỗng."",
    ""ExpectedOutput"": ""Kết quả in ra màn hình Console mong đợi để máy chấm tự động so sánh."",
    ""DifficultyLevel"": ""Easy""
}}";
            chatHistory.AddUserMessage(prompt);

            try
            {
                var response = await _geminiService.GetChatMessageContentAsync(chatHistory);
                string aiRawText = response.ToString() ?? throw new Exception("Lỗi gọi AI để sinh đề bài.");

                // XÓA MARKDOWN BLOCK NẾU CÓ
                if (aiRawText.Contains("```"))
                {
                    aiRawText = aiRawText.Replace("```json", "").Replace("```", "").Trim();
                }

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
                    DifficultyLevel = exerciseData.DifficultyLevel,

                    // MAP CHUẨN NGÔN NGỮ ĐỂ FE & COMPILER SỬ DỤNG
                    Language = targetLanguage.Contains("JavaScript") ? "javascript" :
                               targetLanguage.Contains("Python") ? "python" :
                               targetLanguage.Contains("Java") ? "java" :
                               targetLanguage.Contains("SQL") ? "sql" :
                               targetLanguage.Contains("Bash") ? "bash" :
                               targetLanguage.Contains("C++") ? "cpp" : "csharp"
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
                AssessmentType = "TESTED", // Gán cờ rõ ràng cho bài làm thật
                TakenAt = DateTime.Now
            };

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

            decimal executionScore = 0.0m;
            string codeExecutionFeedback = string.Empty;
            string aiFeedback = "Không thể kết nối đến AI Engine.";

            if (submission.CodeSubmission != null && !string.IsNullOrEmpty(submission.CodeSubmission.SourceCode))
            {
                string clientId = _configuration["JDoodleConfig:ClientId"];
                string clientSecret = _configuration["JDoodleConfig:ClientSecret"];
                string apiUrl = "https://api.jdoodle.com/v1/execute";

                // MỞ RỘNG BỘ NGÔN NGỮ BẮT THEO TÊN CHUẨN CỦA FE & DB CHUYỂN XUỐNG
                string jLanguage = submission.CodeSubmission.Language.ToLower() switch
                {
                    "csharp" => "csharp",
                    "javascript" => "nodejs",
                    "python" => "python3",
                    "java" => "java",
                    "c" => "c",
                    "cpp" => "cpp14",
                    "sql" => "sql",      // Thêm hỗ trợ SQL
                    "bash" => "bash",    // Thêm hỗ trợ Shell Script/Linux
                    _ => throw new Exception($"Ngôn ngữ {submission.CodeSubmission.Language} chưa được hệ thống hỗ trợ chấm tự động.")
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

                // ĐÃ SỬA: Chuyển thẳng sang dùng Gemini làm Reviewer chính thức
                try
                {
                    var response = await _geminiService.GetChatMessageContentAsync(chatHistory);
                    aiFeedback = response.ToString();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n[LỖI GEMINI CODE REVIEW]: {ex.Message}");
                    // Fallback khi cả Gemini cũng sập (rất hiếm khi xảy ra)
                    aiFeedback = "Hệ thống AI đang bảo trì. Không thể đưa ra nhận xét code vào lúc này.";
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
                assessmentType = h.AssessmentType,
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
        // ==========================================
        // FIX: THUẬT TOÁN BỐC ĐỀ ĐÁNH GIÁ ĐẦU VÀO (PLACEMENT TEST)
        // ==========================================
        public async Task<List<QuizQuestionDto>> GetComprehensiveQuizByRoleAsync(int roleId)
        {
            // Lấy tất cả node của nghề này
            var allNodes = await _repository.GetSkillNodesByRoleIdAsync(roleId);
            if (allNodes == null || !allNodes.Any())
                throw new Exception("Chưa có kỹ năng nào được cấu hình cho ngành nghề này.");

            // CHIẾN LƯỢC: Chỉ test 5-7 Node cốt lõi (PriorityLevel nhỏ nhất - Tức là nền tảng)
            // Đừng test mấy cái râu ria ở cuối lộ trình vì nếu cơ bản hổng thì nâng cao chắc chắn hổng.
            var coreNodes = allNodes.OrderBy(n => n.PriorityLevel).Take(6).ToList();

            var finalQuestions = new List<AssessmentQuestion>();
            int questionsPerNode = 3; // Lấy 3 câu mỗi Node Cốt lõi (Tổng ~ 15-18 câu)

            foreach (var node in coreNodes)
            {
                // 1. Tìm câu hỏi trong DB trước
                var dbQuestions = await _repository.GetQuestionsBySkillNodeAsync(node.SkillNodeId, questionsPerNode);

                // 2. Nếu DB thiếu, gọi thẳng Gemini AI để sinh (Không dùng QuizApi nữa)
                if (dbQuestions == null || dbQuestions.Count < questionsPerNode)
                {
                    // Yêu cầu AI đẻ thêm 5 câu cho dư dả
                    dbQuestions = await GenerateAndSaveQuestionsForNodeAsync(node.SkillNodeId, node.NodeName, 5);
                }

                // 3. Add vào đề thi tổng
                if (dbQuestions != null && dbQuestions.Any())
                {
                    finalQuestions.AddRange(dbQuestions.OrderBy(x => Guid.NewGuid()).Take(questionsPerNode));
                }
            }

            // Trộn ngẫu nhiên toàn bộ đề thi trước khi gửi xuống Client
            finalQuestions = finalQuestions.OrderBy(x => Guid.NewGuid()).ToList();

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

            var targetNode = nodes.FirstOrDefault(n => n.IsCodingRequired == true) ?? nodes.First();

            return await GetOrGenerateCodingExerciseAsync(targetNode.SkillNodeId);
        }

        private async Task<List<AssessmentQuestion>> GenerateAndSaveQuestionsForNodeAsync(int skillNodeId, string nodeName, int count)
        {
            try
            {
                var chatHistory = new ChatHistory();
                chatHistory.AddSystemMessage("You are a strict automated JSON array generator. Do not include markdown codeblocks like ```json.");

                string prompt = $@"Bạn là một chuyên gia đào tạo IT cao cấp.
Hãy tạo {count} câu hỏi trắc nghiệm (Multiple Choice) bằng tiếng Việt để kiểm tra tư duy logic về kỹ năng '{nodeName}'.

LƯU Ý QUAN TRỌNG VỀ ĐỊNH DẠNG CODE TRONG JSON:
Nếu câu hỏi yêu cầu đọc hiểu đoạn mã (code), bạn BẮT BUỘC phải gộp toàn bộ đoạn mã đó vào bên trong giá trị của trường ""QuestionText"". 
Tuyệt đối KHÔNG sử dụng phím Enter/xuống dòng thật bên trong chuỗi JSON. Phải sử dụng ký tự `\n` để ngắt dòng cho code. Dùng dấu nháy đơn `'` bên trong code thay vì nháy kép `""` để tránh lỗi JSON.

YÊU CẦU BẮT BUỘC: CHỈ trả về ĐÚNG MỘT mảng JSON hợp lệ, KHÔNG thêm lời chào.
Cấu trúc JSON bắt buộc phải giống hệt ví dụ sau:
[
    {{
        ""QuestionText"": ""Đâu là đầu ra của đoạn mã {nodeName} sau?\n```\nx = 10\nprint(x)\n```"",
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

                // XÓA MARKDOWN BLOCK NẾU CÓ
                if (aiRawText.Contains("```"))
                {
                    aiRawText = aiRawText.Replace("```json", "").Replace("```", "").Trim();
                }

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

        // ==========================================
        // KHAI BÁO NĂNG LỰC ĐẦU VÀO (FR3.1)
        // ==========================================
        // Sửa lại đoạn logic trong SaveSelfDeclaredSkillsAsync
        public async Task<bool> SaveSelfDeclaredSkillsAsync(Guid studentId, List<int> acquiredSkillNodeIds)
        {
            // Sử dụng Transaction để bảo đảm an toàn dữ liệu
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var allHistory = await _repository.GetAssessmentSessionsByStudentAsync(studentId);
                var existingDeclaredSessions = allHistory.Where(h => h.AssessmentType == "SELF_DECLARED").ToList();
                var nodesToKeep = acquiredSkillNodeIds ?? new List<int>();

                // Xóa các session cũ
                foreach (var session in existingDeclaredSessions)
                {
                    if (!nodesToKeep.Contains(session.SkillNodeId))
                    {
                        await _repository.DeleteAssessmentSessionAsync(session.SessionId);
                    }
                }

                // Thêm session mới
                var existingDeclaredNodeIds = existingDeclaredSessions.Select(s => s.SkillNodeId).ToList();
                foreach (var nodeId in nodesToKeep)
                {
                    if (existingDeclaredNodeIds.Contains(nodeId)) continue;

                    var newSession = new AssessmentSession
                    {
                        SessionId = Guid.NewGuid(),
                        StudentId = studentId,
                        SkillNodeId = nodeId,
                        TotalQuizScore = 10.0m,
                        TotalCodeScore = 10.0m,
                        AssessmentType = "SELF_DECLARED",
                        TakenAt = DateTime.Now
                    };
                    await _repository.SaveAssessmentSessionAsync(newSession);
                }

                // Commit toàn bộ nếu thành công
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                // Lỗi thì hủy bỏ toàn bộ thao tác tránh rác DB
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}