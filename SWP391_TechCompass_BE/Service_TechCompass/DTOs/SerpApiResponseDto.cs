using System.Text.Json.Serialization;

namespace Service_TechCompass.DTOs
{
    public class SerpApiResponseDto
    {
        [JsonPropertyName("jobs_results")]
        public List<SerpApiJobDto> JobsResults { get; set; } = new();
    }

    public class SerpApiJobDto
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("company_name")]
        public string CompanyName { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("via")]
        public string Source { get; set; }
    }
}