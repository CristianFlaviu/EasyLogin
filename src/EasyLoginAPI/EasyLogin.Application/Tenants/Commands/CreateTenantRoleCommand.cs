using EasyLogin.Application.Common;
using EasyLogin.Application.Tenants.Dtos;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using MediatR;

namespace EasyLogin.Application.Tenants.Commands;

public record CreateTenantRoleCommand(string Name, string? Description, Guid TenantId) : IRequest<TenantRoleResponse>;

public class CreateTenantRoleCommandHandler(ITenantRoleRepository tenantRoleRepository, IAuditLogger auditLogger)
    : IRequestHandler<CreateTenantRoleCommand, TenantRoleResponse>
{
    public async Task<TenantRoleResponse> Handle(CreateTenantRoleCommand request, CancellationToken cancellationToken)
    {
        TenantRoleResponse role;
        try
        {
            role = await tenantRoleRepository.CreateAsync(request.Name, request.Description, request.TenantId);
        }
        catch (Exception ex)
        {
            await auditLogger.WriteAsync(new AuditEntry
            {
                EventType = AuditEventType.RoleCreateFailed,
                Success = false,
                TargetType = AuditTargetType.TenantRole,
                TargetDisplay = request.Name,
                FailureReason = ex.Message,
                Metadata = new Dictionary<string, string> { ["tenantId"] = request.TenantId.ToString() }
            }, cancellationToken);
            throw;
        }

        await auditLogger.WriteAsync(new AuditEntry
        {
            EventType = AuditEventType.RoleCreated,
            Success = true,
            TargetType = AuditTargetType.TenantRole,
            TargetId = role.Id.ToString(),
            TargetDisplay = role.Name,
            Metadata = new Dictionary<string, string> { ["tenantId"] = request.TenantId.ToString() }
        }, cancellationToken);

        return role;
    }
}
