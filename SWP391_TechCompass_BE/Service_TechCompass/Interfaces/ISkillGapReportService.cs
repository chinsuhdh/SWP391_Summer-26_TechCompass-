using System;
using System.Threading.Tasks;

namespace Service_TechCompass.Interfaces
{
    public interface ISkillGapReportService
    {
        Task<object> GetSkillGapDataAsync(Guid studentId);
        Task<byte[]> GeneratePdfReportAsync(object reportData);

        // BỔ SUNG: Hàm mới chịu trách nhiệm lưu Report vào Database
        Task<(int StatusCode, string Message)> SaveDailySkillGapReportAsync(Guid studentId);
    }
}