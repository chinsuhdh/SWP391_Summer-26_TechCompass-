using System.Text.Json.Serialization;

namespace Service_TechCompass.DTOs.Assessment
{
    public class QuizApiResponseDto
    {
        [JsonPropertyName("question")]
        public string Question { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("answers")]
        public QuizApiAnswers Answers { get; set; }

        [JsonPropertyName("correct_answers")]
        public QuizApiCorrectAnswers CorrectAnswers { get; set; }

        [JsonPropertyName("explanation")]
        public string Explanation { get; set; }

        [JsonPropertyName("difficulty")]
        public string Difficulty { get; set; }
    }

    public class QuizApiAnswers
    {
        [JsonPropertyName("answer_a")] public string AnswerA { get; set; }
        [JsonPropertyName("answer_b")] public string AnswerB { get; set; }
        [JsonPropertyName("answer_c")] public string AnswerC { get; set; }
        [JsonPropertyName("answer_d")] public string AnswerD { get; set; }
    }

    public class QuizApiCorrectAnswers
    {
        [JsonPropertyName("answer_a_correct")] public string AnswerACorrect { get; set; }
        [JsonPropertyName("answer_b_correct")] public string AnswerBCorrect { get; set; }
        [JsonPropertyName("answer_c_correct")] public string AnswerCCorrect { get; set; }
        [JsonPropertyName("answer_d_correct")] public string AnswerDCorrect { get; set; }
    }
}