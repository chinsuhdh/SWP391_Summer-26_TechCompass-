using Repository_TechCompass.Models;

namespace Repository_TechCompass.Interfaces
{
    public interface IContentRepository
    {
        // --- Tech Paths ---
        List<TechPath> GetAllTechPaths();
        TechPath? GetTechPathById(int id);
        void AddTechPath(TechPath entity);
        void UpdateTechPath(TechPath entity);
        void DeleteTechPath(TechPath entity);

        // --- Skill Nodes ---
        List<SkillNode> GetAllSkillNodes();
        SkillNode? GetSkillNodeById(int id);
        void AddSkillNode(SkillNode entity);
        void UpdateSkillNode(SkillNode entity);
        void DeleteSkillNode(SkillNode entity);

        // --- Learning Resources ---
        // Giữ lại đúng tên hàm mà AdminContentService đang gọi
        List<LearningResource> GetAllLearningResources();
        LearningResource? GetLearningResourceById(int id);
        void AddResource(LearningResource entity);
        void UpdateResource(LearningResource entity);
        void DeleteResource(LearningResource entity);

        // --- Save Changes ---
        void SaveChanges();


        Task<SkillNode?> GetSkillNodeByIdAsync(int nodeId);
        Task<List<LearningResource>> GetLearningResourcesByNodeIdAsync(int nodeId);


        Task<LearningResource?> GetLearningResourceByIdAsync(int resourceId);
        Task<RoadmapProgress?> GetRoadmapProgressAsync(Guid studentId, int skillNodeId);
        Task AddRoadmapProgressAsync(RoadmapProgress progress);
    }
}