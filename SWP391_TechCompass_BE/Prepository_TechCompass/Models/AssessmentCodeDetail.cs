using System;

namespace Repository_TechCompass.Models
{
    public partial class AssessmentCodeDetail
    {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public string SourceCode { get; set; } = null!;
        public string? AiFeedback { get; set; }

        public virtual AssessmentSession Session { get; set; } = null!;
    }
}