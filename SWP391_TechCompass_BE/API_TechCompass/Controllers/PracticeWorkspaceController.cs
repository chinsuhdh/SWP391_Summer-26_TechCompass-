using Microsoft.AspNetCore.Mvc;
using Service_TechCompass.DTOs.Practice;
using Service_TechCompass.Interfaces;

namespace API_TechCompass.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class PracticeWorkspaceController : ControllerBase
    {
        private readonly IPracticeWorkspaceService _practiceService;

        public PracticeWorkspaceController(IPracticeWorkspaceService practiceService)
        {
            _practiceService = practiceService;
        }

        // Chức năng 35: Sinh viên bấm nút "Run Code"
        [HttpPost("run-code")]
        public async Task<IActionResult> RunCode([FromBody] RunCodeRequestDto request)
        {
            try
            {
                var result = await _practiceService.RunCodeAsync(request);
                return Ok(new { Message = "Thực thi code hoàn tất.", Data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // Chức năng 37: Sinh viên chat với AI Tutor
        [HttpPost("ai-tutor")]
        public async Task<IActionResult> AskAiTutor([FromBody] AiTutorRequestDto request)
        {
            try
            {
                var result = await _practiceService.ChatWithAiTutorAsync(request);
                return Ok(new { Message = "AI Tutor đã trả lời.", AiResponse = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}