using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using EasyLogin.Domain.Enums;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record EnableEmailTwoFactorCommand(string UserId, string Password) : IRequest;

public class EnableEmailTwoFactorCommandHandler(
    IUserRepository userRepository,
    ITwoFactorService twoFactorService,
    IEmailService emailService,
    IEmailTemplateRenderer templateRenderer,
    IAuditLogger auditLogger)
    : IRequestHandler<EnableEmailTwoFactorCommand>
{
    public async Task Handle(EnableEmailTwoFactorCommand request, CancellationToken cancellationToken)
    {
        ApplicationUser user = await userRepository.GetByIdAsync(request.UserId);
        if (!user.EmailConfirmed)
            throw new InvalidOperationException("Confirm your email address before enabling email 2FA.");

        if (await twoFactorService.IsLockedOutAsync(user.Id))
            throw new UnauthorizedAccessException();

        if (!await twoFactorService.CheckPasswordAsync(user.Id, request.Password))
        {
            await twoFactorService.AccessFailedAsync(user.Id);
            await auditLogger.WriteAsync(new AuditEntry
            {
                EventType = AuditEventType.TwoFactorVerificationFailed,
                Success = false,
                ActorUserId = user.Id,
                ActorEmail = user.Email,
                FailureReason = "InvalidPassword"
            }, cancellationToken);
            throw new UnauthorizedAccessException();
        }

        await twoFactorService.SetTwoFactorEnabledAsync(user.Id, true);
        await twoFactorService.SetTwoFactorMethodAsync(user.Id, TwoFactorMethod.Email);
        await twoFactorService.ResetAccessFailedCountAsync(user.Id);

        string body = await templateRenderer.RenderAsync("TwoFactorEnabled", new Dictionary<string, string>
        {
            ["firstName"] = user.FirstName,
            ["action"] = "enabled"
        });
        await emailService.SendAsync(user.Email, "Two-factor authentication enabled", body);

        await auditLogger.WriteAsync(new AuditEntry
        {
            EventType = AuditEventType.TwoFactorEnabled,
            Success = true,
            ActorUserId = user.Id,
            ActorEmail = user.Email,
            TargetType = AuditTargetType.User,
            TargetId = user.Id,
            TargetDisplay = user.Email
        }, cancellationToken);
    }
}
