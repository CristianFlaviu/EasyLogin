using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using MediatR;

namespace EasyLogin.Application.Tenants.Commands;

public record UpdateTenantUserCommand(
    string UserId, string FirstName, string LastName, string Email,
    bool IsActive, IList<Guid> TenantRoleIds, string? NewPassword,
    Guid CallerTenantId)
    : IRequest<UserDetailResponse>;

public class UpdateTenantUserCommandHandler(
    IUserRepository userRepository,
    ITenantRoleRepository tenantRoleRepository,
    IAuditLogger auditLogger)
    : IRequestHandler<UpdateTenantUserCommand, UserDetailResponse>
{
    public async Task<UserDetailResponse> Handle(UpdateTenantUserCommand request, CancellationToken cancellationToken)
    {
        var (before, _, beforeTenantRoles) = await userRepository.GetByIdWithRolesAsync(request.UserId, request.CallerTenantId);

        try
        {
            await userRepository.UpdateUserAsync(
                request.UserId, request.FirstName, request.LastName,
                request.Email, request.IsActive, null,
                request.NewPassword, request.CallerTenantId);

            await tenantRoleRepository.UpdateUserRolesAsync(
                request.UserId, request.TenantRoleIds, request.CallerTenantId);
        }
        catch (Exception ex)
        {
            await auditLogger.WriteAsync(new AuditEntry
            {
                EventType = AuditEventType.UserUpdateFailed,
                Success = false,
                TargetType = AuditTargetType.User,
                TargetId = request.UserId,
                TargetDisplay = before.Email,
                FailureReason = ex.Message
            }, cancellationToken);
            throw;
        }

        var (user, systemRoles, tenantRoles) = await userRepository.GetByIdWithRolesAsync(request.UserId, request.CallerTenantId);

        var diff = new Dictionary<string, string>();
        AuditDiffBuilder.ForField("firstName", before.FirstName, user.FirstName, diff);
        AuditDiffBuilder.ForField("lastName", before.LastName, user.LastName, diff);
        AuditDiffBuilder.ForField("email", before.Email, user.Email, diff);
        AuditDiffBuilder.ForField("isActive", before.IsActive, user.IsActive, diff);
        AuditDiffBuilder.ForCollection("tenantRoles", beforeTenantRoles, tenantRoles, diff);
        if (!string.IsNullOrWhiteSpace(request.NewPassword))
            diff["password"] = "changed";

        await auditLogger.WriteAsync(new AuditEntry
        {
            EventType = AuditEventType.UserUpdated,
            Success = true,
            TargetType = AuditTargetType.User,
            TargetId = user.Id,
            TargetDisplay = user.Email,
            Metadata = diff.Count > 0 ? diff : null
        }, cancellationToken);

        return new UserDetailResponse(
            user.Id, user.FirstName, user.LastName, user.Email,
            user.IsActive, user.CreatedAt, user.UpdatedAt,
            user.TenantId, user.TenantName,
            systemRoles, tenantRoles, user.Status.ToString());
    }
}
