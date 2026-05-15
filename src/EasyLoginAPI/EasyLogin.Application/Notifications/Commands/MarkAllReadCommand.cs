using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Notifications.Commands;

public record MarkAllReadCommand : IRequest;

public class MarkAllReadCommandHandler(
    ICurrentUserService currentUserService,
    INotificationService notificationService)
    : IRequestHandler<MarkAllReadCommand>
{
    public async Task Handle(MarkAllReadCommand request, CancellationToken cancellationToken)
    {
        string userId = currentUserService.UserId
            ?? throw new UnauthorizedAccessException();

        await notificationService.MarkAllReadAsync(userId, cancellationToken);
    }
}
