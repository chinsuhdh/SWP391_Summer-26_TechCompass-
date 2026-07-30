using System.Collections.Generic;

namespace Service_TechCompass.DTOs
{
    public class DocumentCategoryDto
    {
        public int TargetRoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double MarketDemandIndex { get; set; }

        // Danh sách tài liệu thuộc chủ đề này
        public List<DocumentItemDto> TechPaths { get; set; } = new List<DocumentItemDto>();
    }

    public class DocumentItemDto
    {
        public int TechPathId { get; set; }
        public string PathName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int TotalNodes { get; set; }
    }
}