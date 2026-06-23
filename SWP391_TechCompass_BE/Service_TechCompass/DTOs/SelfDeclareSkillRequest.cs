namespace Service_TechCompass.DTOs
{
    public class SelfDeclareSkillRequest
    {
        // Danh sách các kỹ năng tự khai báo
        public List<DeclaredSkillDto> Skills { get; set; } = new List<DeclaredSkillDto>();
    }

    public class DeclaredSkillDto
    {
        public int SkillNodeId { get; set; }
        // Mức độ thành thạo: 1 (Beginner), 2 (Intermediate), 3 (Advanced)
        public int ProficiencyLevel { get; set; }
    }
}