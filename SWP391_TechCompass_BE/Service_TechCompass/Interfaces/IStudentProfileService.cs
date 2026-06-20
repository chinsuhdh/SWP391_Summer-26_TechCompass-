using Microsoft.AspNetCore.Http;
using Service_TechCompass.DTOs;

namespace Service_TechCompass.Interfaces
{
    public interface IStudentProfileService
    {
        Task<(int StatusCode, string Message, UserStudentProfileDto? Data)> GetProfileAsync(Guid userId);
        Task<(int StatusCode, string Message)> UpdateProfileAsync(Guid userId, UpdateStudentProfileDto request);

        Task<(int StatusCode, string Message, object? Data)> ProcessTranscriptAsync(Guid userId, IFormFile file);
    }
}