using System.Threading.Tasks;

namespace Service_TechCompass.Interfaces
{
    public interface IQuizSyncService
    {
        Task<int> FetchAndSaveQuestionsAsync(int skillNodeId, string tags, int limit = 10);
    }
}