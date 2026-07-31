using System;
using System.Collections.Generic;

namespace Service_TechCompass.DTOs
{
    // DTO Tổng hợp trả về cho Frontend
    public class AdminAiMonitorDashboardDto
    {
        public AiStatsDto Stats { get; set; } = new AiStatsDto();
        public List<AiChatLogDto> ChatLogs { get; set; } = new List<AiChatLogDto>();
    }

    // DTO Thống kê Token & Chi phí
    public class AiStatsDto
    {
        public int TotalRequests { get; set; }
        public int TotalTokensUsed { get; set; }
        public double EstimatedCostUsd { get; set; }
    }

    // DTO Lịch sử từng đoạn Chat
    public class AiChatLogDto
    {
        public Guid LogId { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }
        public string UserPrompt { get; set; }
        public string AiResponse { get; set; }
        public int TokensUsed { get; set; }
        public DateTime CreatedAt { get; set; }
        public string AiModel { get; set; } // VD: "gemini-1.5-flash"
    }
}