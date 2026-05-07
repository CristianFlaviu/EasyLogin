using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Application.Tenants.Dtos;
using EasyLogin.Domain.Entities;
using EasyLogin.Domain.Enums;
using MediatR;

namespace EasyLogin.Application.Tenants.Commands;

public record InviteTenantUserCommand(string Email, Guid TenantRoleId, Guid CallerTenantId) : IRequest<UserDetailResponse>;

public class InviteTenantUserCommandHandler(
    IUserRepository userRepository,
    ITenantRoleRepository tenantRoleRepository,
    ITenantRepository tenantRepository,
    IEmailService emailService,
    IEmailTemplateRenderer templateRenderer,
    IAuditLogger auditLogger,
    IAppUrlProvider appUrlProvider)
    : IRequestHandler<InviteTenantUserCommand, UserDetailResponse>
{
    private const int ExpiryHours = 48;
    private static readonly string[] ForbiddenRoleNames = ["TenantAdmin", "SuperAdmin", "OrgAdmin"];

    public async Task<UserDetailResponse> Handle(InviteTenantUserCommand request, CancellationToken cancellationToken)
    {
        if (!await tenantRepository.IsActiveAsync(request.CallerTenantId))
            throw new InvalidOperationException("Your organization is suspended. Invites are disabled.");

        IList<TenantRoleResponse> tenantRoles = await tenantRoleRepository.GetByTenantIdAsync(request.CallerTenantId);
        TenantRoleResponse selectedRole = tenantRoles.FirstOrDefault(role => role.Id == request.TenantRoleId)
            ?? throw new InvalidOperationException("Selected role does not belong to your organization.");

        EnsureAssignableByTenantAdmin(selectedRole.Name);

        ApplicationUser? existing = await userRepository.GetByEmailAsync(request.Email);
        if (existing is not null && await userRepository.IsInTenantAsync(existing.Id, request.CallerTenantId))
            throw new InvalidOperationException($"Email '{request.Email}' already exists in your organization.");
        if (existing is not null && existing.Status is UserStatus.Suspended or UserStatus.Expired)
            throw new InvalidOperationException("This account cannot be invited in its current state.");

        ApplicationUser user;
        try
        {
            user = existing ?? await userRepository.CreatePendingUserAsync(
                GuessFirstName(request.Email), "Invited", request.Email, request.CallerTenantId);

            await tenantRoleRepository.UpdateUserRolesAsync(user.Id, [selectedRole.Id], request.CallerTenantId);

            string rawToken = Guid.NewGuid().ToString("N");
            string tokenHash = HashHelper.Sha256(rawToken);
            DateTimeOffset expiresAt = DateTimeOffset.UtcNow.AddHours(ExpiryHours);
            await userRepository.StoreInviteTokenAsync(user.Id, tokenHash, expiresAt);

            string frontendBaseUrl = appUrlProvider.FrontendBaseUrl;
            string inviteUrl = $"{frontendBaseUrl.TrimEnd('/')}/accept-invite?token={Uri.EscapeDataString(rawToken)}";
            string body = await templateRenderer.RenderAsync("InviteUser", new Dictionary<string, string>
            {
                ["firstName"] = user.FirstName,
                ["inviteUrl"] = inviteUrl,
                ["expiryHours"] = ExpiryHours.ToString()
            });

            await emailService.SendAsync(request.Email, "You're invited to EasyLogin", body);
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

        (ApplicationUser detail, IList<string> systemRoles, IList<string> assignedRoles) =
            await userRepository.GetByIdWithRolesAsync(user.Id, request.CallerTenantId);

        await auditLogger.WriteAsync(new AuditEntry
        {
            EventType = AuditEventType.UserInvited,
            Success = true,
            TargetType = AuditTargetType.User,
            TargetId = user.Id,
            TargetDisplay = user.Email,
            Metadata = new Dictionary<string, string>
            {
                ["tenantId"] = request.CallerTenantId.ToString(),
                ["tenantRole"] = selectedRole.Name,
                ["expiresInHours"] = ExpiryHours.ToString()
            }
        }, cancellationToken);

        return new UserDetailResponse(
            detail.Id, detail.FirstName, detail.LastName, detail.Email,
            detail.CreatedAt, detail.UpdatedAt,
            detail.TenantId, detail.TenantName,
            systemRoles, assignedRoles, detail.Status.ToDto());
    }

    private static void EnsureAssignableByTenantAdmin(string roleName)
    {
        if (ForbiddenRoleNames.Contains(roleName, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("Org Admin cannot assign roles equal to or above their own role.");
    }

    private static string GuessFirstName(string email)
    {
        string local = email.Split('@', StringSplitOptions.RemoveEmptyEntries)[0];
        if (string.IsNullOrWhiteSpace(local))
            return "there";

        string first = local
            .Split(['.', '_', '-'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? local;

        return char.ToUpperInvariant(first[0]) + first[1..];
    }
}
