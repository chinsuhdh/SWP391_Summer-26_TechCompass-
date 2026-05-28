using System;
using System.Collections.Generic;

namespace Repository_TechCompass.Models;

public partial class ChatMessage
{
    public Guid MessageId { get; set; }

    public Guid? AiSessionId { get; set; }

    public Guid? MentorSessionId { get; set; }

    public string? SenderType { get; set; }

    public string MessageText { get; set; } = null!;

    public DateTime? SentAt { get; set; }

    public virtual AiChatSession? AiSession { get; set; }

    public virtual MentorSession? MentorSession { get; set; }
}
