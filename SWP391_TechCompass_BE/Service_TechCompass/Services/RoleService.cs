using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepo; // Đổi sang dùng RoleRepository

        public RoleService(IRoleRepository roleRepo)
        {
            _roleRepo = roleRepo;
        }

        public Task<(int StatusCode, string Message, List<RoleDto>? Data)> GetAllRolesAsync()
        {
            var roles = _roleRepo.GetAllRoles();
            var data = roles.Select(r => new RoleDto { RoleId = r.RoleId, RoleName = r.RoleName }).ToList();
            return Task.FromResult<(int, string, List<RoleDto>?)>((200, "Lấy danh sách vai trò thành công.", data));
        }

        public Task<(int StatusCode, string Message, RoleDto? Data)> GetRoleByIdAsync(int roleId)
        {
            var role = _roleRepo.GetRoleById(roleId);
            if (role == null) return Task.FromResult<(int, string, RoleDto?)>((404, "Không tìm thấy vai trò.", null));

            var data = new RoleDto { RoleId = role.RoleId, RoleName = role.RoleName };
            return Task.FromResult<(int, string, RoleDto?)>((200, "Tìm thấy vai trò.", data));
        }

        public Task<(int StatusCode, string Message)> CreateRoleAsync(CreateRoleDto request)
        {
            var newRole = new Role { RoleName = request.RoleName };
            _roleRepo.AddRole(newRole);
            _roleRepo.SaveChanges();
            return Task.FromResult<(int, string)>((201, "Tạo vai trò thành công."));
        }

        public Task<(int StatusCode, string Message)> UpdateRoleAsync(int roleId, UpdateRoleDto request)
        {
            var role = _roleRepo.GetRoleById(roleId);
            if (role == null) return Task.FromResult<(int, string)>((404, "Không tìm thấy vai trò để cập nhật."));

            role.RoleName = request.RoleName;
            _roleRepo.UpdateRole(role);
            _roleRepo.SaveChanges();
            return Task.FromResult<(int, string)>((200, "Cập nhật vai trò thành công."));
        }

        public Task<(int StatusCode, string Message)> DeleteRoleAsync(int roleId)
        {
            var role = _roleRepo.GetRoleById(roleId);
            if (role == null) return Task.FromResult<(int, string)>((404, "Không tìm thấy vai trò để xóa."));

            _roleRepo.DeleteRole(role);
            _roleRepo.SaveChanges();
            return Task.FromResult<(int, string)>((200, "Xóa vai trò thành công."));
        }
    }
}