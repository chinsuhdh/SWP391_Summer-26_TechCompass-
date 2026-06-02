using Repository_TechCompass.Models;
using System;
using System.Collections.Generic;

namespace Repository_TechCompass.Interfaces
{
    public interface IAnalyticsRepository
    {
        int GetTotalJobPostings();
        DateTime? GetLastJobScrapedDate();
        List<TrendAnalysis> GetLatestTrends(int limit);
        SkillNode? GetSkillNodeById(int id);

        List<Student> GetAllStudents();
        int CountCompletedNodes(Guid studentId);
    }
}