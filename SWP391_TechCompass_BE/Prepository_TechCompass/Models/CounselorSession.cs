using System;
using System.Collections.Generic;

namespace Repository_TechCompass.Models
{
    public partial class CounselorSession
    {
        public Guid SessionId { get; set; }
        public Guid StudentId { get; set; }
        public Guid CounselorId { get; set; }
        public DateTime StartedAt { get; set; }
        public string? Status { get; set; }

        public virtual Student Student { get; set; } = null!;
        public virtual Counselor Counselor { get; set; } = null!;
        public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
    }
}