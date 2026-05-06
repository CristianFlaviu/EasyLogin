using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record ResendInviteCommand(string UserId) : IRequest;

public class ResendInviteCommandHandler(
    IUserRepository userRepository,
    IEmailService emailService,
    IEmailTemplateRenderer templateRenderer,
    IAuditLogger auditLogger,
    IAppUrlProvider appUrlProvider)
    : IRequestHandler<ResendInviteCommand>
{
    private const int ExpiryHours = 48;

    public async Task Handle(ResendInviteCommand request, CancellationToken cancellationToken)
    {
        ApplicationUser user = await userRepository.GetByIdAsync(request.UserId);
        if (user.Status != UserStatus.Pending)
            throw new InvalidOperationException("Only pending users can receive another invite.");

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
                ["expiresInHours"] = ExpiryHours.ToString()
            }
        }, cancellationToken);
    }
}
