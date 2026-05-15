using EasyLogin.Application.Interfaces;
using EasyLogin.Application.Notifications.Dtos;
using Microsoft.AspNetCore.SignalR;

namespace EasyLogin.Infrastructure.Realtime;

public class SignalRNotificationPusher(IHubContext<NotificationHub> hubContext) : INotificationPusher
{
    public async Task PushToUserAsync(
        string userId,
        NotificationResponse notification,
        CancellationToken cancellationToken = default)
    {
        await hubContext.Clients
            .User(userId)
            .SendAsync("notification", notification, cancellationToken);
    }
}
