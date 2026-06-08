using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Service_TechCompass.Hubs
{
    public class MentorChatHub : Hub
    {
        // Khi client mở màn hình Chat hoặc Dashboard, nó sẽ Join vào 1 Group tương ứng với ID của nó
        public async Task JoinSessionGroup(string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        }

        // Kênh thông báo cá nhân (Task 66)
        public async Task JoinUserGroup(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }
    }
}
