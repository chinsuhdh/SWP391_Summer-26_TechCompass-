using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Service_TechCompass.Hubs
{
    [Authorize]
    public class RoadmapNotificationHub : Hub
    {
        public async Task SubscribeToRoadmapUpdates(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"roadmap_user_{userId}");
        }

        public async Task UnsubscribeFromRoadmapUpdates(string userId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"roadmap_user_{userId}");
        }
    }
}