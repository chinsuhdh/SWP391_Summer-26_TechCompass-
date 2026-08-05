using System;
using System.Collections.Generic;

namespace Service_TechCompass.DTOs
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }

        // Cải tiến an toàn: Đảm bảo PageSize > 0 để tránh lỗi chia cho 0
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    }
}