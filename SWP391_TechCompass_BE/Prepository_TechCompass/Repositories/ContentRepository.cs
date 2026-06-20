using System;
using Microsoft.EntityFrameworkCore;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;

namespace Repository_TechCompass.Repositories
{
    public class ContentRepository : IContentRepository
    {
        private readonly Swp391CareerRoadmapContext _context;

        public ContentRepository(Swp391CareerRoadmapContext context)
        {
            _context = context;
        }

        // Thực thi các hàm (Ví dụ với TechPath, bạn làm tương tự cho Node và Resource)
        public List<TechPath> GetAllTechPaths() => _context.TechPaths.ToList();
        public TechPath? GetTechPathById(int id) => _context.TechPaths.Find(id);
        public void AddTechPath(TechPath entity) => _context.TechPaths.Add(entity);
        public void UpdateTechPath(TechPath entity) => _context.TechPaths.Update(entity);
        public void DeleteTechPath(TechPath entity) => _context.TechPaths.Remove(entity);

        public List<SkillNode> GetAllSkillNodes() => _context.SkillNodes.ToList();
        public SkillNode? GetSkillNodeById(int id) => _context.SkillNodes.Find(id);
        public void AddSkillNode(SkillNode entity) => _context.SkillNodes.Add(entity);
        public void UpdateSkillNode(SkillNode entity) => _context.SkillNodes.Update(entity);
        public void DeleteSkillNode(SkillNode entity) => _context.SkillNodes.Remove(entity);

        public List<LearningResource> GetAllResources() => _context.LearningResources.ToList();
        public LearningResource? GetResourceById(int id) => _context.LearningResources.Find(id);
        public void AddResource(LearningResource entity) => _context.LearningResources.Add(entity);
        public void UpdateResource(LearningResource entity) => _context.LearningResources.Update(entity);
        public void DeleteResource(LearningResource entity) => _context.LearningResources.Remove(entity);
        public List<LearningResource> GetAllLearningResources()
        {
            // Lấy toàn bộ danh sách
            return _context.LearningResources.ToList();
        }

        public LearningResource? GetLearningResourceById(int id)
        {
            // Tìm Resource theo ID
            return _context.LearningResources.FirstOrDefault(r => r.ResourceId == id);
        }

        public async Task<SkillNode?> GetSkillNodeByIdAsync(int nodeId)
        {
            return await _context.SkillNodes
                .FirstOrDefaultAsync(n => n.SkillNodeId == nodeId);
        }

        // Thực thi hàm lấy danh sách tài liệu theo NodeId (Bất đồng bộ)
        public async Task<List<LearningResource>> GetLearningResourcesByNodeIdAsync(int nodeId)
        {
            return await _context.LearningResources
                .Where(r => r.SkillNodeId == nodeId)
                .ToListAsync();
        }

        public void SaveChanges() => _context.SaveChanges();

        // Thêm các hàm này vào trong class ContentRepository

        public async Task<LearningResource?> GetLearningResourceByIdAsync(int resourceId)
        {
            return await _context.LearningResources
                .FirstOrDefaultAsync(r => r.ResourceId == resourceId);
        }

        public async Task<RoadmapProgress?> GetRoadmapProgressAsync(Guid studentId, int skillNodeId)
        {
            return await _context.RoadmapProgresses
                .FirstOrDefaultAsync(p => p.StudentId == studentId && p.SkillNodeId == skillNodeId);
        }

        public async Task AddRoadmapProgressAsync(RoadmapProgress progress)
        {
            await _context.RoadmapProgresses.AddAsync(progress);
        }

    }

}