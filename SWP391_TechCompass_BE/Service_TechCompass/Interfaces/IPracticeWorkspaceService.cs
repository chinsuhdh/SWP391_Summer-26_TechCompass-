using Service_TechCompass.DTOs.Practice;

namespace Service_TechCompass.Interfaces
{
    public interface IPracticeWorkspaceService
    {
        Task<RunCodeResponseDto> RunCodeAsync(RunCodeRequestDto request);
        Task<string> ChatWithAiTutorAsync(AiTutorRequestDto request);
    }
}