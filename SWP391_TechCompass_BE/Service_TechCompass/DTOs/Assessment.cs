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

    public class AssessmentFeedbackDto
    {
        public Guid AssessmentId { get; set; }
        public string? NodeName { get; set; }
        public decimal? TestScore { get; set; }
        public string? AiFeedback { get; set; }
        public DateTime? TakenAt { get; set; }
    }

    public class SelfDeclareSkillDto
    {
        public Guid StudentId { get; set; }
        public List<int> AcquiredSkillNodeIds { get; set; }
    }

    public class SkillDeclarationItem
    {
        public int SkillNodeId { get; set; }

        // Tự đánh giá mức độ tự tin/điểm số (ví dụ: thang 10)
        public decimal SelfAssessedScore { get; set; }
    }

}