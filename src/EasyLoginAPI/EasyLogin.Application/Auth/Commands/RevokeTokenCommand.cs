using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record RevokeTokenCommand(string UserId) : IRequest;

public class RevokeTokenCommandHandler(IUserRepository userRepository, IAuditLogger auditLogger)
    : IRequestHandler<RevokeTokenCommand>
{
    public async Task Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        await userRepository.RevokeRefreshTokenAsync(request.UserId);

        await auditLogger.WriteAsync(new AuditEntry
        {
            EventType = AuditEventType.RevokeToken,
            Success = true,
            ActorUserId = request.UserId
        }, cancellationToken);
    }
}
