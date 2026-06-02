using Service_TechCompass.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service_TechCompass.Interfaces
{
    public interface IAdminMentorService
    {
        Task<(int StatusCode, string Message, List<MentorDto>? Data)> GetAllMentorsAsync();
        Task<(int StatusCode, string Message, MentorDto? Data)> GetMentorByIdAsync(Guid mentorId);
        Task<(int StatusCode, string Message)> CreateMentorAsync(CreateMentorDto request);
        Task<(int StatusCode, string Message)> UpdateMentorAsync(Guid mentorId, UpdateMentorDto request);
        Task<(int StatusCode, string Message)> DeleteMentorAsync(Guid mentorId);
    }
}