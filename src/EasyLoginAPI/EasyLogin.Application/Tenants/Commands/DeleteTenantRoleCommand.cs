using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using MediatR;

namespace EasyLogin.Application.Tenants.Commands;

public record DeleteTenantRoleCommand(Guid RoleId, Guid CallerTenantId) : IRequest;

public class DeleteTenantRoleCommandHandler(ITenantRoleRepository tenantRoleRepository, IAuditLogger auditLogger)
    : IRequestHandler<DeleteTenantRoleCommand>
{
    public async Task Handle(DeleteTenantRoleCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await tenantRoleRepository.DeleteAsync(request.RoleId, request.CallerTenantId);
        }
        catch (Exception ex)
        {
            await auditLogger.WriteAsync(new AuditEntry
            {
                EventType = AuditEventType.RoleDeleteFailed,
                Success = false,
                TargetType = AuditTargetType.TenantRole,
                TargetId = request.RoleId.ToString(),
                FailureReason = ex.Message,
                Metadata = new Dictionary<string, string> { ["tenantId"] = request.CallerTenantId.ToString() }
            }, cancellationToken);
            throw;
        }

        await auditLogger.WriteAsync(new AuditEntry
        {
            EventType = AuditEventType.RoleDeleted,
            Success = true,
            TargetType = AuditTargetType.TenantRole,
            TargetId = request.RoleId.ToString(),
            Metadata = new Dictionary<string, string> { ["tenantId"] = request.CallerTenantId.ToString() }
        }, cancellationToken);
    }
}
