using System.ComponentModel.DataAnnotations;

namespace Service_TechCompass.DTOs
{
    // DTO để sinh viên gửi thông tin bài khảo sát (Survey)
    public class SubmitSurveyDto
    {
        // Bạn có thể tùy biến các câu hỏi này theo Form khảo sát thực tế của hệ thống
        public string ProgrammingLanguagePreference { get; set; } = string.Empty;
        public string ProblemSolvingSkill { get; set; } = string.Empty;
        public string WorkEnvironmentPreference { get; set; } = string.Empty;
        public string CareerGoal { get; set; } = string.Empty;
    }

    // DTO để sinh viên chốt chọn Target Role (Mục tiêu nghề nghiệp)
    public class SelectTargetRoleDto
    {
        [Required(ErrorMessage = "Vui lòng chọn một nghề nghiệp mục tiêu")]
        public int TargetRoleId { get; set; }
    }
}