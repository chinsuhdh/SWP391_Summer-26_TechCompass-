using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace API_TechCompass.Controllers
{
    [Route("api/admin/mentors")]
    [ApiController]
    [Authorize] // Bảo vệ API
    public class AdminMentorController : ControllerBase
    {
        private readonly IAdminMentorService _mentorService;

        public AdminMentorController(IAdminMentorService mentorService)
        {
            _mentorService = mentorService;
        }

        // Kiểm tra xem User gọi API có phải Admin (RoleId = 1) không
        private bool IsAdminUser()
        {
            var roleIdClaim = User.Claims.FirstOrDefault(c => c.Type == "RoleId")?.Value;
            return roleIdClaim == "1";
        }

        [HttpGet]
        public async Task<IActionResult> GetAllMentors()
        {
            if (!IsAdminUser()) return Forbid();
            var result = await _mentorService.GetAllMentorsAsync();
            return StatusCode(result.StatusCode, new { message = result.Message, data = result.Data });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMentorById(Guid id)
        {
            if (!IsAdminUser()) return Forbid();
            var result = await _mentorService.GetMentorByIdAsync(id);
            if (result.StatusCode != 200) return StatusCode(result.StatusCode, new { message = result.Message });
            return Ok(new { message = result.Message, data = result.Data });
        }

        [HttpPost]
        public async Task<IActionResult> CreateMentor([FromBody] CreateMentorDto request)
        {
            if (!IsAdminUser()) return Forbid();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _mentorService.CreateMentorAsync(request);
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMentor(Guid id, [FromBody] UpdateMentorDto request)
        {
            if (!IsAdminUser()) return Forbid();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _mentorService.UpdateMentorAsync(id, request);
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMentor(Guid id)
        {
            if (!IsAdminUser()) return Forbid();
            var result = await _mentorService.DeleteMentorAsync(id);
            return StatusCode(result.StatusCode, new { message = result.Message });
        }
    }
}