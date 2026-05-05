using EasyLogin.Application.Common;
using EasyLogin.Application.Companies.Dtos;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using MediatR;

namespace EasyLogin.Application.Companies.Commands;

public record CreateCompanyRoleCommand(string Name, string? Description, Guid CompanyId) : IRequest<CompanyRoleResponse>;

public class CreateCompanyRoleCommandHandler(ICompanyRoleRepository companyRoleRepository, IAuditLogger auditLogger)
    : IRequestHandler<CreateCompanyRoleCommand, CompanyRoleResponse>
{
    public async Task<CompanyRoleResponse> Handle(CreateCompanyRoleCommand request, CancellationToken cancellationToken)
    {
        CompanyRoleResponse role;
        try
        {
            role = await companyRoleRepository.CreateAsync(request.Name, request.Description, request.CompanyId);
        }
        catch (Exception ex)
        {
            await auditLogger.WriteAsync(new AuditEntry
            {
                EventType = AuditEventType.RoleCreateFailed,
                Success = false,
                TargetType = AuditTargetType.CompanyRole,
                TargetDisplay = request.Name,
                FailureReason = ex.Message,
                Metadata = new Dictionary<string, string> { ["companyId"] = request.CompanyId.ToString() }
            }, cancellationToken);
            throw;
        }

        await auditLogger.WriteAsync(new AuditEntry
        {
            EventType = AuditEventType.RoleCreated,
            Success = true,
            TargetType = AuditTargetType.CompanyRole,
            TargetId = role.Id.ToString(),
            TargetDisplay = role.Name,
            Metadata = new Dictionary<string, string> { ["companyId"] = request.CompanyId.ToString() }
        }, cancellationToken);

        return role;
    }
}
