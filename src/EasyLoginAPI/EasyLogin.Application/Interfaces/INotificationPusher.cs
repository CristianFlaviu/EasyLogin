using EasyLogin.Application.Notifications.Dtos;

namespace EasyLogin.Application.Interfaces;

public interface INotificationPusher
{
    Task PushToUserAsync(
        string userId,
        NotificationResponse notification,
        CancellationToken cancellationToken = default);
}
