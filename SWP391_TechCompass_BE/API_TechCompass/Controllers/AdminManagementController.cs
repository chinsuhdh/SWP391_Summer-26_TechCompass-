using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service_TechCompass.DTOs; // <--- Dòng này được thêm vào để nhận diện DTO
using Service_TechCompass.Interfaces;

namespace API_TechCompass.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize] // Bắt buộc đăng nhập bằng JWT
    public class AdminManagementController : ControllerBase
    {
        private readonly IAdminUserService _adminUserService;
        private readonly IRoleService _roleService;

        public AdminManagementController(IAdminUserService adminUserService, IRoleService roleService)
        {
            _adminUserService = adminUserService;
            _roleService = roleService;
        }

        // Hàm kiểm tra an toàn xem Token gửi lên có thuộc về Role Admin (ID = 1) không
        private bool IsAdminUser()
        {
            var roleIdClaim = User.FindFirst("RoleId")?.Value;
            return roleIdClaim == "1";
        }

        #region CRUD ROLES MANAGEMENT
        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            if (!IsAdminUser()) return StatusCode(403, new { message = "Bạn không có quyền tiếp cận chức năng Admin này." });
            var res = await _roleService.GetAllRolesAsync();
            return Ok(res.Data);
        }

        [HttpGet("roles/{id}")]
        public async Task<IActionResult> GetRoleById(int id)
        {
            if (!IsAdminUser()) return StatusCode(403, new { message = "Không có quyền Admin." });
            var res = await _roleService.GetRoleByIdAsync(id);
            if (res.StatusCode != 200) return StatusCode(res.StatusCode, new { message = res.Message });
            return Ok(res.Data);
        }

        [HttpPost("roles")]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto request)
        {
            if (!IsAdminUser()) return StatusCode(403, new { message = "Không có quyền Admin." });
            var res = await _roleService.CreateRoleAsync(request);
            return StatusCode(res.StatusCode, new { message = res.Message });
        }

        [HttpPut("roles/{id}")]
        public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateRoleDto request)
        {
            if (!IsAdminUser()) return StatusCode(403, new { message = "Không có quyền Admin." });
            var res = await _roleService.UpdateRoleAsync(id, request);
            return StatusCode(res.StatusCode, new { message = res.Message });
        }

        [HttpDelete("roles/{id}")]
        public async Task<IActionResult> DeleteRole(int id)
        {
            if (!IsAdminUser()) return StatusCode(403, new { message = "Không có quyền Admin." });
            var res = await _roleService.DeleteRoleAsync(id);
            return StatusCode(res.StatusCode, new { message = res.Message });
        }
        #endregion

        #region CRUD USERS MANAGEMENT
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            if (!IsAdminUser()) return StatusCode(403, new { message = "Bạn không có quyền tiếp cận chức năng Admin này." });
            var res = await _adminUserService.GetAllUsersAsync();
            return Ok(res.Data);
        }

        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            if (!IsAdminUser()) return StatusCode(403, new { message = "Không có quyền Admin." });
            var res = await _adminUserService.GetUserByIdAsync(id);
            if (res.StatusCode != 200) return StatusCode(res.StatusCode, new { message = res.Message });
            return Ok(res.Data);
        }

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] AdminCreateUserDto request)
        {
            if (!IsAdminUser()) return StatusCode(403, new { message = "Không có quyền Admin." });
            var res = await _adminUserService.CreateUserAsync(request);
            return StatusCode(res.StatusCode, new { message = res.Message });
        }

        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] AdminUpdateUserDto request)
        {
            if (!IsAdminUser()) return StatusCode(403, new { message = "Không có quyền Admin." });
            var res = await _adminUserService.UpdateUserAsync(id, request);
            return StatusCode(res.StatusCode, new { message = res.Message });
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            if (!IsAdminUser()) return StatusCode(403, new { message = "Không có quyền Admin." });
            var res = await _adminUserService.DeleteUserAsync(id);
            return StatusCode(res.StatusCode, new { message = res.Message });
        }
        #endregion
    }
}