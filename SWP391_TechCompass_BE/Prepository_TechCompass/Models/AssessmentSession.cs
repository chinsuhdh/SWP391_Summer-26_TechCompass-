using System;
using System.Collections.Generic;

namespace Repository_TechCompass.Models
{
    public partial class AssessmentSession
    {
        public Guid SessionId { get; set; }
        public Guid StudentId { get; set; }
        public int SkillNodeId { get; set; }
        public decimal TotalQuizScore { get; set; }
        public decimal TotalCodeScore { get; set; }
        public DateTime TakenAt { get; set; }

        public virtual Student Student { get; set; } = null!;
        public virtual SkillNode SkillNode { get; set; } = null!;
        public virtual ICollection<AssessmentQuizDetail> QuizDetails { get; set; } = new List<AssessmentQuizDetail>();
        public virtual AssessmentCodeDetail CodeDetail { get; set; } = null!;
    }
}