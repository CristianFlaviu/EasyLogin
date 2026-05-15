using EasyLogin.Application.Interfaces;
using EasyLogin.Application.Notifications.Dtos;
using EasyLogin.Domain.Entities;
using Mapster;
using MediatR;

namespace EasyLogin.Application.Notifications.Commands;

public record SendTestNotificationCommand(
    string? TargetUserId,
    Guid? TargetTenantId,
    bool Broadcast,
    string Title,
    string Message,
    string Type) : IRequest<int>;

public class SendTestNotificationCommandHandler(
    INotificationService notificationService,
    INotificationPusher notificationPusher)
    : IRequestHandler<SendTestNotificationCommand, int>
{
    public async Task<int> Handle(SendTestNotificationCommand request, CancellationToken cancellationToken)
    {
        if (request.Broadcast)
        {
            List<Notification> notifications = await notificationService.CreateForAllUsersAsync(
                request.Title,
                request.Message,
                request.Type,
                cancellationToken);

            await PushEachAsync(notifications, cancellationToken);
            return notifications.Count;
        }

        if (request.TargetTenantId.HasValue)
        {
            List<Notification> notifications = await notificationService.CreateForTenantUsersAsync(
                request.TargetTenantId.Value,
                request.Title,
                request.Message,
                request.Type,
                cancellationToken);

            await PushEachAsync(notifications, cancellationToken);
            return notifications.Count;
        }

        if (string.IsNullOrWhiteSpace(request.TargetUserId))
            throw new KeyNotFoundException("Target user was not found.");

        Notification notification = await notificationService.CreateAsync(
            request.TargetUserId,
            request.Title,
            request.Message,
            request.Type,
            cancellationToken);

        await notificationPusher.PushToUserAsync(
            notification.UserId,
            notification.Adapt<NotificationResponse>(),
            cancellationToken);

        return 1;
    }

    private async Task PushEachAsync(List<Notification> notifications, CancellationToken cancellationToken)
    {
        foreach (Notification notification in notifications)
        {
            await notificationPusher.PushToUserAsync(
                notification.UserId,
                notification.Adapt<NotificationResponse>(),
                cancellationToken);
        }
    }
}
