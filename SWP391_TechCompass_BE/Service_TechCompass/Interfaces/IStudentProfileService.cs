using Microsoft.AspNetCore.Http;
using Service_TechCompass.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service_TechCompass.Interfaces
{
    public interface IStudentProfileService
    {
        Task<(int StatusCode, string Message, object? Data)> GetProfileAsync(Guid userId);
        Task<(int StatusCode, string Message)> UpdateProfileAsync(Guid userId, UpdateStudentProfileDto request);
        Task<(int StatusCode, string Message, object? Data)> ProcessTranscriptAsync(Guid userId, IFormFile file);
        Task<(int StatusCode, string Message, List<StudentFeedbackDto>? Data)> GetMyFeedbacksAsync(Guid userId);

        // Bổ sung hàm lấy Cố vấn cho sinh viên (Trả về object ẩn danh)
        Task<object?> GetMyCounselorAsync(Guid studentUserId);
    }
}