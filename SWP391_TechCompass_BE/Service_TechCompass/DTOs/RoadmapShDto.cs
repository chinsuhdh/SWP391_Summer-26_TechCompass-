using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Service_TechCompass.DTOs
{
    public class RoadmapShDto
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        // Tùy thuộc vào cấu trúc JSON thực tế, họ thường lưu trong một mảng items hoặc nodes
        [JsonPropertyName("items")]
        public List<RoadmapNodeDto> Items { get; set; }
    }

    public class RoadmapNodeDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("children")]
        public List<RoadmapNodeDto> Children { get; set; }
    }
}