using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Notifications.Commands;

public record MarkNotificationReadCommand(Guid Id) : IRequest;

public class MarkNotificationReadCommandHandler(
    ICurrentUserService currentUserService,
    INotificationService notificationService)
    : IRequestHandler<MarkNotificationReadCommand>
{
    public async Task Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        string userId = currentUserService.UserId
            ?? throw new UnauthorizedAccessException();

        await notificationService.MarkReadAsync(request.Id, userId, cancellationToken);
    }
}
