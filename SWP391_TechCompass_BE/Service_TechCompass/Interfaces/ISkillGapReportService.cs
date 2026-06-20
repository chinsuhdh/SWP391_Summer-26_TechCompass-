using System;
using System.Threading.Tasks;

namespace Service_TechCompass.Interfaces
{
    public interface ISkillGapReportService
    {
        Task<object> GetSkillGapDataAsync(Guid studentId);
        Task<byte[]> GeneratePdfReportAsync(object reportData);
    }
}