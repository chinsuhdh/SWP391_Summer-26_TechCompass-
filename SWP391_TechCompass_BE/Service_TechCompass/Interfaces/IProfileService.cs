using Microsoft.AspNetCore.Http;
using Service_TechCompass.DTOs;
using System;
using System.Threading.Tasks;

namespace Service_TechCompass.Interfaces
{
    public interface IProfileService
    {
        Task<UserProfileDto?> GetProfileMeAsync(Guid userId, int roleId);
        Task<bool> UpdateProfileMeAsync(Guid userId, UpdateStudentProfileDto dto);
        Task<string> UploadTranscriptAsync(Guid userId, IFormFile file);
        Task<object> GetTargetRolesAsync();
    }
}