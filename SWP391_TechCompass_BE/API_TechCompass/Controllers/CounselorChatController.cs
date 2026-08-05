using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Service_TechCompass.Interfaces;

namespace API_TechCompass.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CounselorChatController : ControllerBase
    {
        private readonly ICounselorChatService _chatService;

        public CounselorChatController(ICounselorChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost("session")]
        public async Task<IActionResult> GetSession([FromQuery] Guid studentId, [FromQuery] Guid counselorId)
        {
            try
            {
                var session = await _chatService.GetOrCreateSessionAsync(studentId, counselorId);
                return Ok(new { SessionId = session.SessionId, Status = session.Status });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("history/{sessionId}")]
        public async Task<IActionResult> GetHistory(Guid sessionId)
        {
            try
            {
                var history = await _chatService.GetChatHistoryAsync(sessionId);
                return Ok(history);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}