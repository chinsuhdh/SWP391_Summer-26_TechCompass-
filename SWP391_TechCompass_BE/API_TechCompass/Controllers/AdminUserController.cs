using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;
using System.Threading.Tasks;

namespace API_TechCompass.Controllers
{
    [Route("api/admin/users")]
    [ApiController]
    // [Authorize] // Bật cái này lên nếu cần kiểm tra Token Admin
    public class AdminUserController : ControllerBase
    {
        private readonly IAdminUserService _adminUserService;

        public AdminUserController(IAdminUserService adminUserService)
        {
            _adminUserService = adminUserService;
        }

        [HttpPost("staff")]
        public async Task<IActionResult> CreateStaffAccount([FromBody] AdminCreateAccountDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _adminUserService.CreateStaffAccountAsync(request);

            if (result.StatusCode == 201)
            {
                return Ok(new { message = result.Message });
            }

            return BadRequest(new { message = result.Message });
        }
    }
}