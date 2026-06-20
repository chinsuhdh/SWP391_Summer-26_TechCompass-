namespace Service_TechCompass.DTOs
{
    // DTO đại diện cho 1 tài liệu học tập (Video, Bài báo, Khóa học...)
    public class ResourceDto
    {
        public int ResourceId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ResourceType { get; set; } = string.Empty; // Ví dụ: "Video", "Article", "Quiz", "Course"
        public string Url { get; set; } = string.Empty; // Link dẫn đến tài liệu
        public int EstimatedMinutes { get; set; } // Thời gian dự kiến để hoàn thành (phút)

        public bool IsRegistered { get; set; }
        public string Status { get; set; } = "not-started";
    }

    // DTO gom nhóm các tài liệu theo 1 Node (Kỹ năng)
    public class NodeResourcesDto
    {
        public int NodeId { get; set; }
        public string NodeName { get; set; } = string.Empty;
        public List<ResourceDto> Resources { get; set; } = new List<ResourceDto>();
    }
}