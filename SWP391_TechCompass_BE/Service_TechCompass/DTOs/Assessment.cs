using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Service_TechCompass.DTOs.Assessment
{
    // Object ngoài cùng để hứng chuỗi JSON
    public class QuizApiWrapperDto
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public List<QuizApiQuestionDto> Data { get; set; }
    }

    // DTO hứng từng câu hỏi
    public class QuizApiQuestionDto
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("explanation")]
        public string Explanation { get; set; }

        [JsonPropertyName("difficulty")]
        public string Difficulty { get; set; }

        [JsonPropertyName("answers")]
        public List<QuizApiAnswerItemDto> Answers { get; set; }
    }

    // DTO hứng từng đáp án của 1 câu hỏi
    public class QuizApiAnswerItemDto
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("isCorrect")]
        public bool IsCorrect { get; set; }
    }
}