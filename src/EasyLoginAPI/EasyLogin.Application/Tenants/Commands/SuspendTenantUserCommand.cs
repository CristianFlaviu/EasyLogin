using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using MediatR;

namespace EasyLogin.Application.Tenants.Commands;

public record SuspendTenantUserCommand(string UserId, Guid CallerTenantId, string? CallerUserId) : IRequest;

public class SuspendTenantUserCommandHandler(
    IUserRepository userRepository,
    IAuditLogger auditLogger)
    : IRequestHandler<SuspendTenantUserCommand>
{
    public async Task Handle(SuspendTenantUserCommand request, CancellationToken cancellationToken)
    {
        if (request.CallerUserId == request.UserId)
            throw new InvalidOperationException("You cannot suspend your own account.");

        ApplicationUser user = await userRepository.GetByIdAsync(request.UserId);
        if (!await userRepository.IsInTenantAsync(user.Id, request.CallerTenantId))
            throw new UnauthorizedAccessException();

        await userRepository.UpdateUserAsync(
            user.Id, user.FirstName, user.LastName, user.Email,
            false, null, null, request.CallerTenantId);

        await auditLogger.WriteAsync(new AuditEntry
        {
            EventType = AuditEventType.UserSuspended,
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
