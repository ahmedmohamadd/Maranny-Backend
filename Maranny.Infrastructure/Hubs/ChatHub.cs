using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Maranny.Infrastructure.Hubs
{
    public class ChatHub : Hub
    {
        // Store user connections (userId -> connectionId)
        private static readonly Dictionary<int, string> _userConnections = new();

        public override async Task OnConnectedAsync()
        {
            // Get user ID from query string
            var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString();

            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int userIdInt))
            {
                _userConnections[userIdInt] = Context.ConnectionId;

                // Notify others that user is online
                await Clients.Others.SendAsync("UserOnline", userIdInt);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Remove user connection
            var userToRemove = _userConnections.FirstOrDefault(x => x.Value == Context.ConnectionId);
            if (userToRemove.Key != 0)
            {
                _userConnections.Remove(userToRemove.Key);

                // Notify others that user is offline
                await Clients.Others.SendAsync("UserOffline", userToRemove.Key);
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Send typing indicator
        public async Task SendTypingIndicator(int receiverId)
        {
            var senderUserId = Context.GetHttpContext()?.Request.Query["userId"].ToString();

            if (!string.IsNullOrEmpty(senderUserId) && int.TryParse(senderUserId, out int senderId))
            {
                if (_userConnections.TryGetValue(receiverId, out string? connectionId))
                {
                    await Clients.Client(connectionId).SendAsync("UserTyping", senderId);
                }
            }
        }

        // Send message to specific user
        public static async Task SendMessageToUser(IHubContext<ChatHub> hubContext, int receiverId, object message)
        {
            if (_userConnections.TryGetValue(receiverId, out string? connectionId))
            {
                await hubContext.Clients.Client(connectionId).SendAsync("ReceiveMessage", message);
            }
        }

        // Check if user is online
        public static bool IsUserOnline(int userId)
        {
            return _userConnections.ContainsKey(userId);
        }
    }
}