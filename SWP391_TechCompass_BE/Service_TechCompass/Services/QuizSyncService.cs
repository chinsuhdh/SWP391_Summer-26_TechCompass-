using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class QuizSyncService : IQuizSyncService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IAssessmentRepository _repository;

        public QuizSyncService(HttpClient httpClient, IConfiguration configuration, IAssessmentRepository repository)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _repository = repository;
        }

        public async Task<int> FetchAndSaveQuestionsAsync(int skillNodeId, string tags, int limit = 10)
        {
            string apiTag = tags.ToLower() switch
            {
                var t when t.Contains("c#") || t.Contains("oop") => "c#",
                var t when t.Contains("sql") || t.Contains("relational") => "mysql",
                var t when t.Contains("javascript") || t.Contains("dom") => "javascript",
                var t when t.Contains("react") || t.Contains("hooks") => "react",
                var t when t.Contains("html") => "html",
                var t when t.Contains("css") => "css",
                _ => "linux"
            };

            string apiKey = _configuration["QuizApiConfig:ApiKey"] ?? throw new Exception("Thiếu ApiKey");
            string baseUrl = _configuration["QuizApiConfig:BaseUrl"] ?? throw new Exception("Thiếu BaseUrl");

            string requestUrl = $"{baseUrl}?api_key={apiKey}&limit={limit}&tags={apiTag}";
            try
            {
                var response = await _httpClient.GetAsync(requestUrl);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[QuizAPI Error {response.StatusCode}]");
                    return 0;
                }

                var content = await response.Content.ReadAsStringAsync();

                // Bóc tách JSON linh hoạt
                JsonDocument doc;
                try
                {
                    doc = JsonDocument.Parse(content);
                }
                catch
                {
                    Console.WriteLine("[Error]: Không thể parse JSON");
                    return 0;
                }

                using (doc)
                {
                    JsonElement root = doc.RootElement;
                    List<QuizApiQuestionDto> quizList = null;
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        quizList = JsonSerializer.Deserialize<List<QuizApiQuestionDto>>(content, options);
                    }
                    else if (root.TryGetProperty("data", out var dataElement))
                    {
                        quizList = JsonSerializer.Deserialize<List<QuizApiQuestionDto>>(dataElement.GetRawText(), options);
                    }
                    else if (root.TryGetProperty("results", out var resElement))
                    {
                        quizList = JsonSerializer.Deserialize<List<QuizApiQuestionDto>>(resElement.GetRawText(), options);
                    }

                    if (quizList == null || quizList.Count == 0) return 0;

                    var newQuestions = new List<AssessmentQuestion>();

                    foreach (var q in quizList)
                    {
                        // Lấy đáp án đúng một cách an toàn
                        string correctAns = "A";
                        if (q.correct_answers.HasValue && q.correct_answers.Value.ValueKind == JsonValueKind.Object)
                        {
                            var ca = q.correct_answers.Value;

                            // Hỗ trợ cả trường hợp API trả về boolean true/false hoặc chuỗi "true"/"false"
                            bool IsCorrect(JsonElement element) =>
                                element.ValueKind == JsonValueKind.True ||
                                (element.ValueKind == JsonValueKind.String && element.GetString()?.ToLower() == "true");

                            if (ca.TryGetProperty("answer_a_correct", out var a) && IsCorrect(a)) correctAns = "A";
                            else if (ca.TryGetProperty("answer_b_correct", out var b) && IsCorrect(b)) correctAns = "B";
                            else if (ca.TryGetProperty("answer_c_correct", out var c) && IsCorrect(c)) correctAns = "C";
                            else if (ca.TryGetProperty("answer_d_correct", out var d) && IsCorrect(d)) correctAns = "D";
                        }

                        // Lấy các Option một cách an toàn
                        string optA = null, optB = null, optC = null, optD = null;
                        if (q.answers.HasValue && q.answers.Value.ValueKind == JsonValueKind.Object)
                        {
                            var ans = q.answers.Value;
                            optA = ans.TryGetProperty("answer_a", out var a) && a.ValueKind == JsonValueKind.String ? a.GetString() : null;
                            optB = ans.TryGetProperty("answer_b", out var b) && b.ValueKind == JsonValueKind.String ? b.GetString() : null;
                            optC = ans.TryGetProperty("answer_c", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString() : null;
                            optD = ans.TryGetProperty("answer_d", out var d) && d.ValueKind == JsonValueKind.String ? d.GetString() : null;
                        }

                        var entity = new AssessmentQuestion
                        {
                            SkillNodeId = skillNodeId,
                            QuestionText = q.question ?? q.text ?? "Câu hỏi không xác định?",
                            OptionA = optA,
                            OptionB = optB,
                            OptionC = optC,
                            OptionD = optD,
                            CorrectAnswer = correctAns,
                            Explanation = q.explanation ?? "Không có giải thích chi tiết.",
                            DifficultyLevel = q.difficulty ?? "Easy"
                        };

                        // Đảm bảo lúc nào cũng có ít nhất 2 đáp án (tránh lỗi UI về sau)
                        if (string.IsNullOrEmpty(entity.OptionA)) entity.OptionA = "True / Đúng";
                        if (string.IsNullOrEmpty(entity.OptionB)) entity.OptionB = "False / Sai";

                        newQuestions.Add(entity);
                    }

                    await _repository.SaveQuestionsAsync(newQuestions);
                    return newQuestions.Count;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Sync Exception]: {ex.Message}");
                return 0;
            }
        }

        // LỚP DTO BULLETPROOF
        private class QuizApiQuestionDto
        {
            public string? id { get; set; }
            public string? question { get; set; }
            public string? text { get; set; }
            public string? description { get; set; }
            public string? explanation { get; set; }
            public string? difficulty { get; set; }

            // Thay đổi sang JsonElement? để hứng bất kỳ định dạng nào API quăng ra (Object, Array, Null...)
            public JsonElement? answers { get; set; }
            public JsonElement? correct_answers { get; set; }
        }
    }
}