using System;
using System.Collections.Generic;

namespace Service_TechCompass.DTOs.Assessment
{
    public class SubmitFullExamDto
    {
        public Guid StudentId { get; set; }
        public int SkillNodeId { get; set; }

        // Mảng các câu trả lời trắc nghiệm
        public List<QuizAnswerDto> QuizAnswers { get; set; } = new List<QuizAnswerDto>();

        // Code bài thực hành
        public CodeTestSubmissionDto CodeSubmission { get; set; } = null!;
    }
}