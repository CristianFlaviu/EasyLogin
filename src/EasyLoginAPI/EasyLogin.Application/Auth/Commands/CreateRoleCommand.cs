using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record CreateRoleCommand(string Name, string? Description) : IRequest<RoleResponse>;

public class CreateRoleCommandHandler(IRoleRepository roleRepository, IAuditLogger auditLogger)
    : IRequestHandler<CreateRoleCommand, RoleResponse>
{
    public async Task<RoleResponse> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        RoleResponse role;
        try
        {
            role = await roleRepository.CreateRoleAsync(request.Name, request.Description);
        }
        catch (Exception ex)
        {
            await auditLogger.WriteAsync(new AuditEntry
            {
                EventType = AuditEventType.RoleCreateFailed,
                Success = false,
                TargetType = AuditTargetType.Role,
                TargetDisplay = request.Name,
                FailureReason = ex.Message
            }, cancellationToken);
            throw;
        }

        await auditLogger.WriteAsync(new AuditEntry
        {
            EventType = AuditEventType.RoleCreated,
            Success = true,
            TargetType = AuditTargetType.Role,
            TargetId = role.Id,
            TargetDisplay = role.Name,
            Metadata = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : new Dictionary<string, string> { ["description"] = request.Description }
        }, cancellationToken);

        return role;
    }
}
