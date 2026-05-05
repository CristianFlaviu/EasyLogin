using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record DeleteRoleCommand(string RoleId) : IRequest;

public class DeleteRoleCommandHandler(IRoleRepository roleRepository, IAuditLogger auditLogger)
    : IRequestHandler<DeleteRoleCommand>
{
    public async Task Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await roleRepository.DeleteRoleAsync(request.RoleId);
        }
        catch (Exception ex)
        {
            await auditLogger.WriteAsync(new AuditEntry
            {
                EventType = AuditEventType.RoleDeleteFailed,
                Success = false,
                TargetType = AuditTargetType.Role,
                TargetId = request.RoleId,
                FailureReason = ex.Message
            }, cancellationToken);
            throw;
        }

        await auditLogger.WriteAsync(new AuditEntry
        {
            EventType = AuditEventType.RoleDeleted,
            Success = true,
            TargetType = AuditTargetType.Role,
            TargetId = request.RoleId
        }, cancellationToken);
    }
}
