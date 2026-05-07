using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record VerifyTwoFactorCommand(string TwoFactorToken, string Code) : IRequest<AuthResponse>;

public class VerifyTwoFactorCommandHandler(
    IUserRepository userRepository,
    ITwoFactorService twoFactorService,
    ITokenService tokenService,
    IAuditLogger auditLogger)
    : IRequestHandler<VerifyTwoFactorCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(VerifyTwoFactorCommand request, CancellationToken cancellationToken)
    {
        TwoFactorChallengePrincipal challenge;
        try
        {
            challenge = tokenService.ValidateTwoFactorChallengeToken(request.TwoFactorToken);
        }
        catch
        {
            await auditLogger.WriteAsync(new AuditEntry
            {
                EventType = AuditEventType.TwoFactorVerificationFailed,
                Success = false,
                FailureReason = "InvalidChallengeToken"
            }, cancellationToken);
            throw new UnauthorizedAccessException();
        }

        ApplicationUser user = await userRepository.GetByIdAsync(challenge.UserId);
        if (user.Status != UserStatus.Active || !user.TwoFactorEnabled)
        {
            await auditLogger.WriteAsync(new AuditEntry
            {
                EventType = AuditEventType.TwoFactorVerificationFailed,
                Success = false,
                ActorUserId = user.Id,
                ActorEmail = user.Email,
                FailureReason = "InvalidUserState"
            }, cancellationToken);
            throw new UnauthorizedAccessException();
        }

        if (await twoFactorService.IsLockedOutAsync(user.Id))
        {
            await auditLogger.WriteAsync(new AuditEntry
            {
                EventType = AuditEventType.TwoFactorVerificationFailed,
                Success = false,
                ActorUserId = user.Id,
                ActorEmail = user.Email,
                FailureReason = "LockedOut"
            }, cancellationToken);
            throw new UnauthorizedAccessException();
        }

        TwoFactorMethod method = user.TwoFactorMethod ?? TwoFactorMethod.Authenticator;
        bool verified = method == TwoFactorMethod.Email
            ? await TwoFactorCommandHelpers.VerifyEmailCodeAsync(user.Id, request.Code, twoFactorService)
            : await TwoFactorCommandHelpers.VerifyAuthenticatorCodeAsync(user.Id, request.Code, twoFactorService);

        if (!verified)
        {
            await twoFactorService.AccessFailedAsync(user.Id);
            string failureReason = await twoFactorService.IsLockedOutAsync(user.Id)
                ? "LockedOut"
                : "InvalidTwoFactorCode";

            await auditLogger.WriteAsync(new AuditEntry
            {
                EventType = AuditEventType.TwoFactorVerificationFailed,
                Success = false,
                ActorUserId = user.Id,
                ActorEmail = user.Email,
                FailureReason = failureReason
            }, cancellationToken);
            throw new UnauthorizedAccessException();
        }

        await twoFactorService.ResetAccessFailedCountAsync(user.Id);

        (ApplicationUser mappedUser, IList<string> roles, _) = await userRepository.GetByIdWithRolesAsync(user.Id);
        AccessTokenResult accessToken = tokenService.GenerateAccessToken(mappedUser, roles);
        string rawRefreshToken = tokenService.GenerateRefreshToken();
        DateTimeOffset expiry = DateTimeOffset.UtcNow.AddDays(7);

        await userRepository.StoreRefreshTokenAsync(mappedUser.Id, HashHelper.Sha256(rawRefreshToken), expiry);

        await auditLogger.WriteAsync(new AuditEntry
        {
            EventType = AuditEventType.TwoFactorVerificationSuccess,
            Success = true,
            ActorUserId = mappedUser.Id,
            ActorEmail = mappedUser.Email,
            Jti = accessToken.Jti
        }, cancellationToken);

        await auditLogger.WriteAsync(new AuditEntry
        {
            EventType = AuditEventType.LoginSuccess,
            Success = true,
            ActorUserId = mappedUser.Id,
            ActorEmail = mappedUser.Email,
            Jti = accessToken.Jti
        }, cancellationToken);

        return new AuthResponse(accessToken.Token, rawRefreshToken, tokenService.AccessTokenExpiryMinutes * 60);
    }
}
