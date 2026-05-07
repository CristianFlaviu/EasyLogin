using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using EasyLogin.Domain.Enums;
using MediatR;

namespace EasyLogin.Application.Tenants.Commands;

public record ResendTenantInviteCommand(string UserId, Guid CallerTenantId) : IRequest;

public class ResendTenantInviteCommandHandler(
    IUserRepository userRepository,
    ITenantRepository tenantRepository,
    IEmailService emailService,
    IEmailTemplateRenderer templateRenderer,
    IAuditLogger auditLogger,
    IAppUrlProvider appUrlProvider)
    : IRequestHandler<ResendTenantInviteCommand>
{
    private const int ExpiryHours = 48;

    public async Task Handle(ResendTenantInviteCommand request, CancellationToken cancellationToken)
    {
        if (!await tenantRepository.IsActiveAsync(request.CallerTenantId))
            throw new InvalidOperationException("Your organization is suspended. Invites are disabled.");

        ApplicationUser user = await userRepository.GetByIdAsync(request.UserId);
        if (!await userRepository.IsInTenantAsync(user.Id, request.CallerTenantId))
            throw new UnauthorizedAccessException();

        if (user.Status is UserStatus.Suspended or UserStatus.Expired)
            throw new InvalidOperationException("Only pending or active users can receive another invite.");

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

        await emailService.SendAsync(user.Email, "Your EasyLogin invite", body);

        await auditLogger.WriteAsync(new AuditEntry
        {
            EventType = AuditEventType.UserInviteResent,
            Success = true,
            TargetType = AuditTargetType.User,
            TargetId = user.Id,
            TargetDisplay = user.Email,
            Metadata = new Dictionary<string, string>
            {
                ["tenantId"] = request.CallerTenantId.ToString(),
                ["expiresInHours"] = ExpiryHours.ToString()
            }
        }, cancellationToken);
    }
}
