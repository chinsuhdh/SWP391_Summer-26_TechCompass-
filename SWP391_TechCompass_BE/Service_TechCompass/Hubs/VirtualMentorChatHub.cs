using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Service_TechCompass.Hubs
{
    [Authorize]
    public class VirtualMentorChatHub : Hub
    {
        public async Task JoinChatSession(string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_session_{sessionId}");
        }

        public async Task LeaveChatSession(string sessionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat_session_{sessionId}");
        }
    }
}