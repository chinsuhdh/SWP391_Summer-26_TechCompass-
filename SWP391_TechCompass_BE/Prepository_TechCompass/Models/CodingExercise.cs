using System;
using System.Collections.Generic;

namespace Repository_TechCompass.Models
{
    public partial class CodingExercise
    {
        public int ExerciseId { get; set; }
        public int SkillNodeId { get; set; }
        public string Title { get; set; } = null!;
        public string ProblemDescription { get; set; } = null!;
        public string? DefaultCodeTemplate { get; set; }
        public string? TestStdin { get; set; }
        public string? ExpectedOutput { get; set; }
        public string? DifficultyLevel { get; set; }

        // DÒNG MỚI THÊM VÀO:
        public string? Language { get; set; }

        public virtual SkillNode SkillNode { get; set; } = null!;
    }
}