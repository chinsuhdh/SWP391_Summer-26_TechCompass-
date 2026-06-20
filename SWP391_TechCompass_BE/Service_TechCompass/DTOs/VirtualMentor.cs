namespace Service_TechCompass.DTOs.VirtualMentor
{
    public class VirtualMentorChatRequestDto
    {
        public Guid? SessionId { get; set; }
        public string UserMessage { get; set; } = string.Empty;
    }

    public class ChatHistoryResponseDto
    {
        public Guid MessageId { get; set; }
        public string SenderType { get; set; } = string.Empty; // "Student" hoặc "AI"
        public string MessageText { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }
    public class ChatSessionListDto
    {
        public Guid SessionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
    }

}