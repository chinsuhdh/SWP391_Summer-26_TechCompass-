namespace Service_TechCompass.DTOs.Practice
{
    // DTO cho Run Code
    public class RunCodeRequestDto
    {
        public string Language { get; set; } = "csharp";
        public string SourceCode { get; set; } = string.Empty;
        public string Stdin { get; set; } = string.Empty;
    }

    public class RunCodeResponseDto
    {
        public string Output { get; set; } = string.Empty;
        public bool IsError { get; set; }
    }

    // DTO cho AI Tutor
    public class AiTutorRequestDto
    {
        public Guid StudentId { get; set; }
        public string ProblemDescription { get; set; } = string.Empty;
        public string CurrentCode { get; set; } = string.Empty;
        public string UserMessage { get; set; } = string.Empty;
    }
}