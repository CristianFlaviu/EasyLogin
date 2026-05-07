using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record DisableTwoFactorCommand(string UserId, string Password, string Code) : IRequest;

public class DisableTwoFactorCommandHandler(
    IUserRepository userRepository,
    ITwoFactorService twoFactorService,
    IEmailService emailService,
    IEmailTemplateRenderer templateRenderer,
    IAuditLogger auditLogger)
    : IRequestHandler<DisableTwoFactorCommand>
{
    public async Task Handle(DisableTwoFactorCommand request, CancellationToken cancellationToken)
    {
        ApplicationUser user = await userRepository.GetByIdAsync(request.UserId);
        if (!user.TwoFactorEnabled)
            throw new InvalidOperationException("Two-factor authentication is not enabled.");

        if (await twoFactorService.IsLockedOutAsync(user.Id))
            throw new TwoFactorVerificationFailedException("LockedOut", isLockedOut: true);

        if (!await twoFactorService.CheckPasswordAsync(user.Id, request.Password))
        {
            bool lockedOut = await RecordFailureAsync(user, "InvalidPassword", cancellationToken);
            throw new TwoFactorVerificationFailedException("InvalidPassword", lockedOut);
        }

        TwoFactorMethod method = user.TwoFactorMethod ?? TwoFactorMethod.Authenticator;
        bool verified = method == TwoFactorMethod.Email
            ? await TwoFactorCommandHelpers.VerifyEmailCodeAsync(user.Id, request.Code, twoFactorService)
            : await TwoFactorCommandHelpers.VerifyAuthenticatorCodeAsync(user.Id, request.Code, twoFactorService);

        if (!verified)
        {
            bool lockedOut = await RecordFailureAsync(user, "InvalidTwoFactorCode", cancellationToken);
            throw new TwoFactorVerificationFailedException("InvalidTwoFactorCode", lockedOut);
        }

        await twoFactorService.SetTwoFactorEnabledAsync(user.Id, false);
        await twoFactorService.SetTwoFactorMethodAsync(user.Id, null);
        await twoFactorService.ResetAuthenticatorAsync(user.Id);
        await twoFactorService.ResetAccessFailedCountAsync(user.Id);

        string body = await templateRenderer.RenderAsync("TwoFactorEnabled", new Dictionary<string, string>
        {
            ["firstName"] = user.FirstName,
            ["action"] = "disabled"
        });
        await emailService.SendAsync(user.Email, "Two-factor authentication disabled", body);

        await auditLogger.WriteAsync(new AuditEntry
        {
            EventType = AuditEventType.TwoFactorDisabled,
            Success = true,
            ActorUserId = user.Id,
            ActorEmail = user.Email,
            TargetType = AuditTargetType.User,
            TargetId = user.Id,
            TargetDisplay = user.Email
        }, cancellationToken);
    }

    private async Task<bool> RecordFailureAsync(ApplicationUser user, string reason, CancellationToken cancellationToken)
    {
        await twoFactorService.AccessFailedAsync(user.Id);
        bool lockedOut = await twoFactorService.IsLockedOutAsync(user.Id);
        string failureReason = lockedOut ? "LockedOut" : reason;
        await auditLogger.WriteAsync(new AuditEntry
        {
            EventType = AuditEventType.TwoFactorVerificationFailed,
            Success = false,
            ActorUserId = user.Id,
            ActorEmail = user.Email,
            FailureReason = failureReason
        }, cancellationToken);
        return lockedOut;
    }
}
