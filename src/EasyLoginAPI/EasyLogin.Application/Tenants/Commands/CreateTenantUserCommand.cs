using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using MediatR;

namespace EasyLogin.Application.Tenants.Commands;

public record CreateTenantUserCommand(
    string FirstName, string LastName, string Email, string Password,
    IList<Guid> TenantRoleIds, Guid CallerTenantId)
    : IRequest<UserDetailResponse>;

public class CreateTenantUserCommandHandler(
    IUserRepository userRepository,
    ITenantRoleRepository tenantRoleRepository,
    IEmailService emailService,
    IEmailTemplateRenderer templateRenderer,
    IAuditLogger auditLogger)
    : IRequestHandler<CreateTenantUserCommand, UserDetailResponse>
{
    private static readonly string[] ForbiddenRoleNames = ["TenantAdmin", "SuperAdmin", "OrgAdmin"];

    public async Task<UserDetailResponse> Handle(CreateTenantUserCommand request, CancellationToken cancellationToken)
    {
        await ValidateAssignableRolesAsync(request.TenantRoleIds, request.CallerTenantId);

        if (await userRepository.EmailExistsAsync(request.Email))
        {
            await auditLogger.WriteAsync(new AuditEntry
            {
                EventType = AuditEventType.UserCreateFailed,
                Success = false,
                TargetType = AuditTargetType.User,
                TargetDisplay = request.Email,
                FailureReason = "EmailAlreadyRegistered"
            }, cancellationToken);
            throw new InvalidOperationException($"Email '{request.Email}' is already registered.");
        }

        Domain.Entities.ApplicationUser user;
        try
        {
            user = await userRepository.CreateUserAsync(
                request.FirstName, request.LastName, request.Email, request.Password, request.CallerTenantId);

            if (request.TenantRoleIds.Count > 0)
                await tenantRoleRepository.UpdateUserRolesAsync(user.Id, request.TenantRoleIds, request.CallerTenantId);
        }
        catch (Exception ex)
        {
            await auditLogger.WriteAsync(new AuditEntry
            {
                EventType = AuditEventType.UserCreateFailed,
                Success = false,
                TargetType = AuditTargetType.User,
                TargetDisplay = request.Email,
                FailureReason = ex.Message
            }, cancellationToken);
            throw;
        }

        var body = await templateRenderer.RenderAsync("Welcome", new Dictionary<string, string>
        {
            ["firstName"] = request.FirstName,
            ["email"] = request.Email
        });
        await emailService.SendAsync(request.Email, "Welcome to EasyLogin", body);

        var (detail, systemRoles, tenantRoles) = await userRepository.GetByIdWithRolesAsync(user.Id, request.CallerTenantId);

        await auditLogger.WriteAsync(new AuditEntry
        {
            EventType = AuditEventType.UserCreated,
            Success = true,
            TargetType = AuditTargetType.User,
            TargetId = user.Id,
            TargetDisplay = user.Email,
            Metadata = new Dictionary<string, string>
            {
                ["tenantId"] = request.CallerTenantId.ToString(),
                ["tenantRoles"] = string.Join(',', tenantRoles)
            }
        }, cancellationToken);

        return new UserDetailResponse(
            detail.Id, detail.FirstName, detail.LastName, detail.Email,
            detail.CreatedAt, detail.UpdatedAt,
            detail.TenantId, detail.TenantName,
            systemRoles, tenantRoles, detail.Status.ToDto());
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
