using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Notifications.Queries;

public record GetUnreadCountQuery : IRequest<int>;

public class GetUnreadCountQueryHandler(
    ICurrentUserService currentUserService,
    INotificationService notificationService)
    : IRequestHandler<GetUnreadCountQuery, int>
{
    public async Task<int> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        string userId = currentUserService.UserId
            ?? throw new UnauthorizedAccessException();

        return await notificationService.GetUnreadCountAsync(userId, cancellationToken);
    }
}
