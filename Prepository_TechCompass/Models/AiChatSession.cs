using System;
using System.Collections.Generic;

namespace Repository_TechCompass.Models;

public partial class AiChatSession
{
    public Guid AiSessionId { get; set; }

    public Guid StudentId { get; set; }

    public DateTime? StartedAt { get; set; }

    public string? ContextType { get; set; }

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    public virtual Student Student { get; set; } = null!;
}
