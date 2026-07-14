
using System;

namespace Repository_TechCompass.Models
{
    public partial class Counselor
    {
        public Guid CounselorId { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string? Department { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual User User { get; set; } = null!;
    }
}


