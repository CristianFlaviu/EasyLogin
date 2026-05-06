using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using MediatR;

namespace EasyLogin.Application.Tenants.Commands;

public record RevokeTenantInviteCommand(string UserId, Guid CallerTenantId) : IRequest;

public class RevokeTenantInviteCommandHandler(
    IUserRepository userRepository,
    IAuditLogger auditLogger)
    : IRequestHandler<RevokeTenantInviteCommand>
{
    public async Task Handle(RevokeTenantInviteCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId);
        if (!await userRepository.IsInTenantAsync(user.Id, request.CallerTenantId))
            throw new UnauthorizedAccessException();

        await userRepository.RevokeInviteTokensAsync(user.Id);

        await auditLogger.WriteAsync(new AuditEntry
        {
            EventType = AuditEventType.UserInviteRevoked,
            Success = true,
            TargetType = AuditTargetType.User,
            TargetId = user.Id,
            TargetDisplay = user.Email,
            Metadata = new Dictionary<string, string>
            {
                ["tenantId"] = request.CallerTenantId.ToString()
            }
        }, cancellationToken);
    }
}
