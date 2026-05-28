using System;
using System.Collections.Generic;

namespace Repository_TechCompass.Models;

public partial class MentorSession
{
    public Guid SessionId { get; set; }

    public Guid MentorId { get; set; }

    public Guid StudentId { get; set; }

    public DateTime? ScheduledAt { get; set; }

    public int? DurationMinutes { get; set; }

    public string? Status { get; set; }

    public string? PaymentStatus { get; set; }

    public string? MeetingLink { get; set; }

    public string? ReviewNotes { get; set; }

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    public virtual Mentor Mentor { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
