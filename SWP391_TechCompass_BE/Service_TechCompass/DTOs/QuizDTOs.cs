using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Service_TechCompass.DTOs.Assessment
{
    public class QuizQuestionDto
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; }
        public Dictionary<string, string> Options { get; set; }
    }

    public class QuizAnswerDto
    {
        public int QuestionId { get; set; }
        public string SelectedOption { get; set; }
    }

    public class QuizSubmissionDto
    {
        public Guid StudentId { get; set; }
        public int SkillNodeId { get; set; }
        public List<QuizAnswerDto> Answers { get; set; }
    }

    public class CodeTestSubmissionDto
    {
        public Guid StudentId { get; set; }
        public int SkillNodeId { get; set; }
        public string Language { get; set; } = "csharp";
        public string SourceCode { get; set; } = string.Empty;
        public string ProblemDescription { get; set; } = string.Empty;

        // Bổ sung Test Cases để Judge0 có thể chấm điểm
        public string? Stdin { get; set; } // Đầu vào giả lập (VD: "5\n10")
        public string? ExpectedOutput { get; set; } // Kết quả mong đợi (VD: "15")
    }

    // --- CÁC DTO DÀNH CHO JUDGE0 API ---
    public class Judge0RequestDto
    {
        [JsonPropertyName("source_code")]
        public string SourceCode { get; set; }

        [JsonPropertyName("language_id")]
        public int LanguageId { get; set; }

        [JsonPropertyName("stdin")]
        public string? Stdin { get; set; }

        [JsonPropertyName("expected_output")]
        public string? ExpectedOutput { get; set; }
    }

    public class Judge0ResponseDto
    {
        [JsonPropertyName("stdout")]
        public string? StdOut { get; set; }

        [JsonPropertyName("stderr")]
        public string? StdErr { get; set; }

        [JsonPropertyName("compile_output")]
        public string? CompileOutput { get; set; }

        [JsonPropertyName("status")]
        public Judge0StatusDto? Status { get; set; }
    }

    public class Judge0StatusDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}