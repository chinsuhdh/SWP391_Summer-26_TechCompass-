using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs.Assessment;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class QuizSyncService : IQuizSyncService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IAssessmentRepository _repository;

        // DI Inject HttpClient, Configuration và Repository vào đây
        public QuizSyncService(HttpClient httpClient, IConfiguration configuration, IAssessmentRepository repository)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _repository = repository;
        }

        public async Task<int> FetchAndSaveQuestionsAsync(int skillNodeId, string tags, int limit = 10)
        {
            // 1. Đọc API Key và BaseUrl từ appsettings.json
            string apiKey = _configuration["QuizApiConfig:ApiKey"];
            string baseUrl = _configuration["QuizApiConfig:BaseUrl"];

            // 2. Thiết lập Header cho HttpClient (Sử dụng chuẩn Bearer Token)
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            // 3. Gọi API
            string requestUrl = $"{baseUrl}?limit={limit}&tags={tags}";
            var response = await _httpClient.GetAsync(requestUrl);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Lỗi khi gọi QuizAPI: " + response.ReasonPhrase);
            }

            var content = await response.Content.ReadAsStringAsync();

            // Cấu hình tùy chọn JsonSerializer để xử lý hoa thường linh hoạt (tùy chọn)
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // 4. Đọc JSON vào Wrapper DTO mới
            var wrapper = JsonSerializer.Deserialize<QuizApiWrapperDto>(content, options);

            // Nếu lỗi phân tích, hoặc API báo false, hoặc không có dữ liệu trả về thì dừng
            if (wrapper == null || !wrapper.Success || wrapper.Data == null || wrapper.Data.Count == 0)
            {
                return 0;
            }

            // 5. Map dữ liệu từ DTO sang Entity của Database
            var newQuestions = new List<AssessmentQuestion>();

            foreach (var q in wrapper.Data)
            {
                // Thuật toán bóc tách mảng answers thành 4 cột A, B, C, D
                string[] optionLabels = { "A", "B", "C", "D" };
                string correctAns = "A"; // Mặc định
                string optA = null, optB = null, optC = null, optD = null;

                // Xử lý tối đa 4 đáp án
                for (int i = 0; i < q.Answers.Count && i < 4; i++)
                {
                    if (i == 0) optA = q.Answers[i].Text;
                    if (i == 1) optB = q.Answers[i].Text;
                    if (i == 2) optC = q.Answers[i].Text;
                    if (i == 3) optD = q.Answers[i].Text;

                    // Nếu isCorrect = true, gán nhãn A/B/C/D tương ứng làm đáp án đúng
                    if (q.Answers[i].IsCorrect)
                    {
                        correctAns = optionLabels[i];
                    }
                }

                // Map vào Entity để Entity Framework lưu xuống SQL
                var entity = new AssessmentQuestion
                {
                    SkillNodeId = skillNodeId,
                    QuestionText = q.Text,
                    OptionA = optA,
                    OptionB = optB,
                    OptionC = optC,
                    OptionD = optD,
                    CorrectAnswer = correctAns,
                    Explanation = q.Explanation ?? "Không có giải thích chi tiết.",
                    DifficultyLevel = q.Difficulty
                };

                newQuestions.Add(entity);
            }

            // 6. Lưu vào Database
            await _repository.SaveQuestionsAsync(newQuestions);

            return newQuestions.Count;
        }
    }
}