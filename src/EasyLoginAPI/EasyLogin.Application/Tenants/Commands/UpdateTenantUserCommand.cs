using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using EasyLogin.Domain.Enums;
using MediatR;

namespace EasyLogin.Application.Tenants.Commands;

public record UpdateTenantUserCommand(
    string UserId, string FirstName, string LastName, string Email,
    UserStatus Status, IList<Guid> TenantRoleIds, string? NewPassword,
    Guid CallerTenantId)
    : IRequest<UserDetailResponse>;

public class UpdateTenantUserCommandHandler(
    IUserRepository userRepository,
    ITenantRoleRepository tenantRoleRepository,
    IAuditLogger auditLogger)
    : IRequestHandler<UpdateTenantUserCommand, UserDetailResponse>
{
    private static readonly string[] ForbiddenRoleNames = ["TenantAdmin", "SuperAdmin", "OrgAdmin"];

    public async Task<UserDetailResponse> Handle(UpdateTenantUserCommand request, CancellationToken cancellationToken)
    {
        await ValidateAssignableRolesAsync(request.TenantRoleIds, request.CallerTenantId);

        var (before, _, beforeTenantRoles) = await userRepository.GetByIdWithRolesAsync(request.UserId, request.CallerTenantId);

        try
        {
            await userRepository.UpdateUserAsync(
                request.UserId, request.FirstName, request.LastName,
                request.Email, request.Status, null,
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
        AuditDiffBuilder.ForField("status", before.Status, user.Status, diff);
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
            user.CreatedAt, user.UpdatedAt,
            user.TenantId, user.TenantName,
            systemRoles, tenantRoles, user.Status.ToDto());
    }

    private async Task ValidateAssignableRolesAsync(IList<Guid> tenantRoleIds, Guid tenantId)
    {
        if (tenantRoleIds.Count == 0)
            return;

        IList<Tenants.Dtos.TenantRoleResponse> tenantRoles = await tenantRoleRepository.GetByTenantIdAsync(tenantId);
        Dictionary<Guid, string> byId = tenantRoles.ToDictionary(role => role.Id, role => role.Name);

        foreach (Guid roleId in tenantRoleIds)
        {
            if (!byId.TryGetValue(roleId, out string? roleName))
                throw new InvalidOperationException("One or more roles do not belong to this tenant.");

            if (ForbiddenRoleNames.Contains(roleName, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("Org Admin cannot assign roles equal to or above their own role.");
        }
    }
}
