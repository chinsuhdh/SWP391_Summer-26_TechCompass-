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
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId.ToLower());
        }

        public async Task SendMessage(Guid sessionId, Guid senderId, string content, bool isFromStudent)
        {
            // 1. Lưu DB
            var savedMessage = await _chatService.SaveMessageAsync(sessionId, senderId, content, isFromStudent);

            // 2. Đẩy tin nhắn realtime với định dạng chuẩn khớp 100% với Database API
            await Clients.Group(sessionId.ToString().ToLower()).SendAsync("ReceiveMessage", new
            {
                messageId = savedMessage.MessageId,
                messageText = savedMessage.MessageText, // Đã đổi thành messageText
                senderType = savedMessage.SenderType,   // Đã đổi thành senderType
                sentAt = savedMessage.SentAt            // Đã đổi thành sentAt
            });
        }
    }
}