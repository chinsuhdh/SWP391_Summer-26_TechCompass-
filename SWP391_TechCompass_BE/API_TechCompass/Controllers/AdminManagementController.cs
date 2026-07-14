using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;
using System;
using System.Threading.Tasks;

namespace API_TechCompass.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize] // Bật Authorize để bảo mật API
    public class AdminManagementController : ControllerBase
    {
        private readonly IAdminUserService _adminUserService;

        public AdminManagementController(IAdminUserService adminUserService)
        {
            _adminUserService = adminUserService;
        }

        private bool IsAdminUser()
        {
            var roleIdClaim = User.FindFirst("RoleId")?.Value;
            return roleIdClaim == "1";
        }

        #region CRUD USERS MANAGEMENT

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            if (!IsAdminUser()) return StatusCode(403, new { message = "Bạn không có quyền truy cập." });

            var res = await _adminUserService.GetAllUsersAsync();
            return Ok(new { message = res.Message, data = res.Data });
        }

        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            if (!IsAdminUser()) return StatusCode(403, new { message = "Bạn không có quyền truy cập." });

            var res = await _adminUserService.GetUserByIdAsync(id);
            return StatusCode(res.StatusCode, new { message = res.Message, data = res.Data });
        }

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] AdminCreateUserDto request)
        {
            if (!IsAdminUser()) return StatusCode(403, new { message = "Bạn không có quyền truy cập." });
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var res = await _adminUserService.CreateUserAsync(request);
            return StatusCode(res.StatusCode, new { message = res.Message });
        }

        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] AdminUpdateUserDto request)
        {
            if (!IsAdminUser()) return StatusCode(403, new { message = "Bạn không có quyền truy cập." });
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var res = await _adminUserService.UpdateUserAsync(id, request);
            return StatusCode(res.StatusCode, new { message = res.Message });
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            if (!IsAdminUser()) return StatusCode(403, new { message = "Bạn không có quyền truy cập." });

            var res = await _adminUserService.DeleteUserAsync(id);
            return StatusCode(res.StatusCode, new { message = res.Message });
        }
        #endregion
    }
}
