using Service_TechCompass.DTOs;

namespace Service_TechCompass.Interfaces
{
    public interface IProfileService
    {
        Task<(int StatusCode, string Message, UserProfileDto? Data)> GetProfileAsync(Guid userId);
        Task<(int StatusCode, string Message)> UpdateProfileAsync(Guid userId, UpdateProfileDto request);
    }
}