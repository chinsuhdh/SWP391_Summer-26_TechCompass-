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
    // [Authorize] // Bật cái này lên nếu cần kiểm tra Token Admin
    public class AdminManagementController : ControllerBase
    {
        private readonly IAdminUserService _adminUserService;
        private readonly IRoleService _roleService;

        public AdminManagementController(IAdminUserService adminUserService, IRoleService roleService)
        {
            _adminUserService = adminUserService;
            _roleService = roleService;
        }

        // Helper check Token Role Admin
        private bool IsAdminUser()
        {
            var roleIdClaim = User.FindFirst("RoleId")?.Value;
            return roleIdClaim == "1";
        }

        #region CRUD ROLES MANAGEMENT
        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            var res = await _roleService.GetAllRolesAsync();
            return Ok(res.Data);
        }

        [HttpGet("roles/{id}")]
        public async Task<IActionResult> GetRoleById(int id)
        {
            var res = await _roleService.GetRoleByIdAsync(id);
            if (res.StatusCode != 200) return StatusCode(res.StatusCode, new { message = res.Message });
            return Ok(res.Data);
        }

        [HttpPost("roles")]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto request)
        {
            var res = await _roleService.CreateRoleAsync(request);
            return StatusCode(res.StatusCode, new { message = res.Message });
        }

        [HttpPut("roles/{id}")]
        public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateRoleDto request)
        {
            var res = await _roleService.UpdateRoleAsync(id, request);
            return StatusCode(res.StatusCode, new { message = res.Message });
        }

        [HttpDelete("roles/{id}")]
        public async Task<IActionResult> DeleteRole(int id)
        {
            var res = await _roleService.DeleteRoleAsync(id);
            return StatusCode(res.StatusCode, new { message = res.Message });
        }
        #endregion

        #region CRUD USERS MANAGEMENT

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var res = await _adminUserService.GetAllUsersAsync();
            return Ok(new { message = res.Message, data = res.Data });
        }

        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            var res = await _adminUserService.GetUserByIdAsync(id);
            return StatusCode(res.StatusCode, new { message = res.Message, data = res.Data });
        }

        // ĐÃ SỬA: Chuẩn RESTful cho phương thức tạo mới
        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] AdminCreateUserDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var res = await _adminUserService.CreateUserAsync(request);
            return StatusCode(res.StatusCode, new { message = res.Message });
        }

        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] AdminUpdateUserDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var res = await _adminUserService.UpdateUserAsync(id, request);
            return StatusCode(res.StatusCode, new { message = res.Message });
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var res = await _adminUserService.DeleteUserAsync(id);
            return StatusCode(res.StatusCode, new { message = res.Message });
        }
        #endregion
    }
}