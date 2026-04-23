using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Maranny.Infrastructure.Hubs
{
    public class NotificationHub : Hub
    {
        // Connection management
        private static readonly Dictionary<int, string> _userConnections = new();

        public override async Task OnConnectedAsync()
        {
            // Get user ID from connection (will be set by client with access token)
            var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString();

            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int userIdInt))
            {
                _userConnections[userIdInt] = Context.ConnectionId;
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
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Method to send notification to specific user
        public static async Task SendNotificationToUser(IHubContext<NotificationHub> hubContext, int userId, object notification)
        {
            if (_userConnections.TryGetValue(userId, out string? connectionId))
            {
                await hubContext.Clients.Client(connectionId).SendAsync("ReceiveNotification", notification);
            }
        }

        // Method to send notification to multiple users
        public static async Task SendNotificationToUsers(IHubContext<NotificationHub> hubContext, List<int> userIds, object notification)
        {
            var connectionIds = userIds
                .Where(id => _userConnections.ContainsKey(id))
                .Select(id => _userConnections[id])
                .ToList();

            if (connectionIds.Any())
            {
                await hubContext.Clients.Clients(connectionIds).SendAsync("ReceiveNotification", notification);
            }
        }
    }
}