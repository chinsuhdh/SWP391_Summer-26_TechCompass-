using Microsoft.AspNetCore.Mvc;
using Service_TechCompass.Interfaces;

namespace API_TechCompass.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AiTalentController : ControllerBase
    {
        private readonly IAiTalentService _aiTalentService;

        public AiTalentController(IAiTalentService aiTalentService)
        {
            _aiTalentService = aiTalentService;
        }

        // POST: api/v1/AiTalent/{studentId}/generate -> Chức năng 27 (Thường được gọi bởi CronJob hoặc Admin)
        [HttpPost("{studentId}/generate")]
        public async Task<IActionResult> GenerateTalentAnalysis(Guid studentId)
        {
            try
            {
                var result = await _aiTalentService.GenerateLatentTalentAsync(studentId);
                return Ok(new { message = "AI Assessment hoàn tất", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // GET: api/v1/AiTalent/{studentId} -> Chức năng 28 (Client/Web gọi để hiển thị cho sinh viên)
        [HttpGet("{studentId}")]
        public async Task<IActionResult> GetTalentAnalysis(Guid studentId)
        {
            try
            {
                var result = await _aiTalentService.GetTalentAnalysisAsync(studentId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }
    }
}