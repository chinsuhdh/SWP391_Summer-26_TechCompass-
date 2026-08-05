using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ICounselorChatService _chatService;

        public ChatHub(ICounselorChatService chatService)
        {
            _chatService = chatService;
        }

        public async Task JoinSession(string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        }

        public async Task SendMessage(Guid sessionId, Guid senderId, string content, bool isFromStudent)
        {
            // 1. Lưu DB
            var savedMessage = await _chatService.SaveMessageAsync(sessionId, senderId, content, isFromStudent);

            // 2. Đẩy tin nhắn realtime tới các user trong phiên chat
            await Clients.Group(sessionId.ToString()).SendAsync("ReceiveMessage", new
            {
                MessageId = savedMessage.MessageId,
                SenderId = senderId,
                Content = savedMessage.MessageText, // Lấy từ MessageText
                Timestamp = savedMessage.SentAt,    // Lấy từ SentAt
                IsFromStudent = savedMessage.SenderType == "Student" // Tính toán lại từ SenderType
            });
        }
    }
}