using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record ConfirmTwoFactorCommand(string UserId, string Code) : IRequest;

public class ConfirmTwoFactorCommandHandler(
    IUserRepository userRepository,
    ITwoFactorService twoFactorService,
    IEmailService emailService,
    IEmailTemplateRenderer templateRenderer,
    IAuditLogger auditLogger)
    : IRequestHandler<ConfirmTwoFactorCommand>
{
    public async Task Handle(ConfirmTwoFactorCommand request, CancellationToken cancellationToken)
    {
        ApplicationUser user = await userRepository.GetByIdAsync(request.UserId);
        if (user.TwoFactorEnabled)
            throw new InvalidOperationException("Two-factor authentication is already enabled.");

        if (await twoFactorService.IsLockedOutAsync(user.Id))
            throw new UnauthorizedAccessException();

        if (!await twoFactorService.VerifyAuthenticatorCodeAsync(user.Id, request.Code))
        {
            await twoFactorService.AccessFailedAsync(user.Id);
            await auditLogger.WriteAsync(new AuditEntry
            {
                EventType = AuditEventType.TwoFactorVerificationFailed,
                Success = false,
                ActorUserId = user.Id,
                ActorEmail = user.Email,
                FailureReason = "InvalidTwoFactorCode"
            }, cancellationToken);
            throw new UnauthorizedAccessException();
        }

        await twoFactorService.SetTwoFactorEnabledAsync(user.Id, true);
        await twoFactorService.SetTwoFactorMethodAsync(user.Id, TwoFactorMethod.Authenticator);
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
