using System;

namespace Service_TechCompass.DTOs
{
    public class CounselorInfoResponseDto
    {
        public Guid CounselorId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
    }
}