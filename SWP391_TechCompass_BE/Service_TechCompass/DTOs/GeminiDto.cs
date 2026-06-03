using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Service_TechCompass.DTOs
{
    // Dữ liệu gửi lên Gemini
    public class GeminiRequestDto
    {
        [JsonPropertyName("contents")]
        public List<GeminiContentDto> Contents { get; set; } = new();
    }

    public class GeminiContentDto
    {
        [JsonPropertyName("parts")]
        public List<GeminiPartDto> Parts { get; set; } = new();
    }

    public class GeminiPartDto
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    // Dữ liệu nhận về từ Gemini
    public class GeminiResponseDto
    {
        [JsonPropertyName("candidates")]
        public List<GeminiCandidateDto>? Candidates { get; set; }
    }

    public class GeminiCandidateDto
    {
        [JsonPropertyName("content")]
        public GeminiContentDto? Content { get; set; }
    }
}