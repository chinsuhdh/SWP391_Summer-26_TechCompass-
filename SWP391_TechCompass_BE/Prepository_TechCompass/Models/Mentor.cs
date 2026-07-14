using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Repository_TechCompass.Models
{
    public partial class Mentor
    {
        public Guid MentorId { get; set; }

        public Guid UserId { get; set; }

        // ĐÃ XÓA [NotMapped] Ở ĐÂY
        public string? FullName { get; set; }

        public string? CurrentCompany { get; set; }

        public string? ExpertiseTags { get; set; }

        public string? LinkedinUrl { get; set; }

        public virtual ICollection<MentorSession> MentorSessions { get; set; } = new List<MentorSession>();

        public virtual User User { get; set; } = null!;
    }
}
