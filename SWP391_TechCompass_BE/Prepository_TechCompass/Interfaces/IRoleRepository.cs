using Repository_TechCompass.Models;

namespace Repository_TechCompass.Interfaces
{
    public interface IRoleRepository
    {
        // Tra cứu (Select) từng role và tất cả role
        List<Role> GetAllRoles();
        Role? GetRoleById(int roleId);

        // Các thao tác CRUD khác
        void AddRole(Role role);
        void UpdateRole(Role role);
        void DeleteRole(Role role);
        void SaveChanges();
    }
}