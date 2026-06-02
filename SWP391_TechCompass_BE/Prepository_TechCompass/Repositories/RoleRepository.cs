using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;

namespace Repository_TechCompass.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly Swp391CareerRoadmapContext _context;

        public RoleRepository(Swp391CareerRoadmapContext context)
        {
            _context = context;
        }

        // Select hiển thị tất cả các Role
        public List<Role> GetAllRoles()
        {
            return _context.Roles.ToList();
        }

        // Select chi tiết từng Role theo ID
        public Role? GetRoleById(int roleId)
        {
            return _context.Roles.FirstOrDefault(r => r.RoleId == roleId);
        }

        public void AddRole(Role role)
        {
            _context.Roles.Add(role);
        }

        public void UpdateRole(Role role)
        {
            _context.Roles.Update(role);
        }

        public void DeleteRole(Role role)
        {
            _context.Roles.Remove(role);
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }
    }
}