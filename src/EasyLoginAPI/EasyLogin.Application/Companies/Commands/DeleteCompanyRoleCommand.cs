using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using MediatR;

namespace EasyLogin.Application.Companies.Commands;

public record DeleteCompanyRoleCommand(Guid RoleId, Guid CallerCompanyId) : IRequest;

public class DeleteCompanyRoleCommandHandler(ICompanyRoleRepository companyRoleRepository, IAuditLogger auditLogger)
    : IRequestHandler<DeleteCompanyRoleCommand>
{
    public async Task Handle(DeleteCompanyRoleCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await companyRoleRepository.DeleteAsync(request.RoleId, request.CallerCompanyId);
        }
        catch (Exception ex)
        {
            await auditLogger.WriteAsync(new AuditEntry
            {
                EventType = AuditEventType.RoleDeleteFailed,
                Success = false,
                TargetType = AuditTargetType.CompanyRole,
                TargetId = request.RoleId.ToString(),
                FailureReason = ex.Message,
                Metadata = new Dictionary<string, string> { ["companyId"] = request.CallerCompanyId.ToString() }
            }, cancellationToken);
            throw;
        }

        await auditLogger.WriteAsync(new AuditEntry
        {
            EventType = AuditEventType.RoleDeleted,
            Success = true,
            TargetType = AuditTargetType.CompanyRole,
            TargetId = request.RoleId.ToString(),
            Metadata = new Dictionary<string, string> { ["companyId"] = request.CallerCompanyId.ToString() }
        }, cancellationToken);
    }
}
