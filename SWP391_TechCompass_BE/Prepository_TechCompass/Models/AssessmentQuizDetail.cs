using System;

namespace Repository_TechCompass.Models
{
    public partial class AssessmentQuizDetail
    {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public int QuestionId { get; set; }
        public string SelectedOption { get; set; } = null!;
        public bool IsCorrect { get; set; }

        public virtual AssessmentSession Session { get; set; } = null!;
        public virtual AssessmentQuestion Question { get; set; } = null!;
    }
}