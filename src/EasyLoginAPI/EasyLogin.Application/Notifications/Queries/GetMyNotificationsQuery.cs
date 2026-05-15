using EasyLogin.Application.Interfaces;
using EasyLogin.Application.Notifications.Dtos;
using Mapster;
using MediatR;

namespace EasyLogin.Application.Notifications.Queries;

public record GetMyNotificationsQuery(bool UnreadOnly, int Skip, int Take)
    : IRequest<List<NotificationResponse>>;

public class GetMyNotificationsQueryHandler(
    ICurrentUserService currentUserService,
    INotificationService notificationService)
    : IRequestHandler<GetMyNotificationsQuery, List<NotificationResponse>>
{
    public async Task<List<NotificationResponse>> Handle(
        GetMyNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        string userId = currentUserService.UserId
            ?? throw new UnauthorizedAccessException();

        List<Domain.Entities.Notification> notifications =
            await notificationService.GetForUserAsync(
                userId,
                request.UnreadOnly,
                request.Skip,
                request.Take,
                cancellationToken);

        return notifications.Adapt<List<NotificationResponse>>();
    }
}
