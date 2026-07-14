using System;

namespace Service_TechCompass.DTOs
{
    public class MentorProfileDto
    {
        public Guid MentorId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string CurrentCompany { get; set; } = string.Empty;
        public string ExpertiseTags { get; set; } = string.Empty;
    }

    public class PublicStudentPortfolioDto
    {
        public Guid PortfolioId { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string TargetRole { get; set; } = string.Empty;
        public string ShareableUrl { get; set; } = string.Empty;
        public string AiProfileSummary { get; set; } = string.Empty;
    }

    public class SubmitFeedbackRequestDto
    {
        public string ReviewNotes { get; set; } = string.Empty;
    }
    public class MentorFeedbackHistoryDto
    {
        public Guid SessionId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string ShareableUrl { get; set; } = string.Empty;
        public string ReviewNotes { get; set; } = string.Empty;
        public DateTime? ScheduledAt { get; set; }
    }
}

