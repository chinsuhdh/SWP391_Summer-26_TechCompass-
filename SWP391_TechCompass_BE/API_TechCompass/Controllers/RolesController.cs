// API_TechCompass/Controllers/RolesController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Repository_TechCompass;
using Repository_TechCompass.Models; // Sửa theo namespace Models của bạn

namespace API_TechCompass.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly Swp391CareerRoadmapContext _context;

        public RolesController(Swp391CareerRoadmapContext context)
        {
            _context = context;
        }

        // 1. API Lấy danh sách các Ngành nghề (Role)
        [HttpGet]
        public async Task<IActionResult> GetTargetRoles()
        {
            var roles = await _context.TargetCareerRoles
                .Select(r => new {
                    id = r.TargetRoleId,
                    name = r.RoleName,
                    description = r.Description,
                    demandIndex = r.MarketDemandIndex
                })
                .ToListAsync();
            return Ok(new { data = roles });
        }

        // 2. API Lấy danh sách Skill Nodes dựa vào Role ID
        [HttpGet("{roleId}/skills")]
        public async Task<IActionResult> GetSkillsByRole(int roleId)
        {
            // Tìm lộ trình (TechPath) ứng với Role
            var techPath = await _context.TechPaths.FirstOrDefaultAsync(tp => tp.TargetRoleId == roleId);
            if (techPath == null) return NotFound(new { message = "Chưa có lộ trình cho ngành này." });

            // Lấy tất cả các kỹ năng (Skill Nodes) thuộc lộ trình này
            var skills = await _context.SkillNodes
                .Where(sn => sn.TechPathId == techPath.TechPathId)
                .OrderBy(sn => sn.PriorityLevel) // Sắp xếp từ dễ đến khó
                .Select(sn => sn.SkillNodeId)
                .ToListAsync();

            return Ok(new { data = skills }); // Trả về mảng các ID: [1, 2, 3, 4]
        }
    }
}