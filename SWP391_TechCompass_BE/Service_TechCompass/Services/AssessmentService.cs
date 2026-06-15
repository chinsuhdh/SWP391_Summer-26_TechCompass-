using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

            string jBaseUrl = _configuration["Judge0Config:BaseUrl"];
            string jApiKey = _configuration["Judge0Config:ApiKey"];
            string jApiHost = _configuration["Judge0Config:ApiHost"];

            int langId = submission.Language.ToLower() switch
            {
                "csharp" => 51,
                "javascript" => 63,
                "python" => 71,
                "java" => 62,
                "c" => 50,
                "cpp" => 54,
                _ => 51
            };

            var judge0Req = new Judge0RequestDto
            {
                SourceCode = submission.SourceCode,
                LanguageId = langId,
                Stdin = submission.Stdin,
                ExpectedOutput = submission.ExpectedOutput
            };

            var jRequestMessage = new HttpRequestMessage(HttpMethod.Post, jBaseUrl);
            jRequestMessage.Headers.Add("X-RapidAPI-Key", jApiKey);
            jRequestMessage.Headers.Add("X-RapidAPI-Host", jApiHost);
            jRequestMessage.Content = new StringContent(JsonSerializer.Serialize(judge0Req), Encoding.UTF8, "application/json");

            var jResponse = await _httpClient.SendAsync(jRequestMessage);

            if (jResponse.IsSuccessStatusCode)
            {
                var jResultString = await jResponse.Content.ReadAsStringAsync();
                var jResult = JsonSerializer.Deserialize<Judge0ResponseDto>(jResultString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (jResult?.Status?.Id == 3)
                {
                    executionScore = 10.0m;
                    codeExecutionFeedback = "Code chạy hoàn hảo, Pass toàn bộ Test Cases.";
                }
                else
                {
                    executionScore = 0.0m;
                    codeExecutionFeedback = $"Trạng thái: {jResult?.Status?.Description}. Lỗi: {jResult?.CompileOutput ?? jResult?.StdErr ?? "Sai kết quả đầu ra."}";
                }
            }
            else
            {
                codeExecutionFeedback = "Lỗi hệ thống: Không thể kết nối với Compiler.";
            }

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

            var gResponse = await _httpClient.PostAsync(gRequestUrl, content);
            string aiFeedback = "Không thể kết nối đến AI Engine.";

            if (gResponse.IsSuccessStatusCode)
            {
                var responseData = await gResponse.Content.ReadAsStringAsync();
                var geminiResponse = JsonSerializer.Deserialize<GeminiResponseDto>(responseData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                aiFeedback = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? aiFeedback;
            }

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
    }
}