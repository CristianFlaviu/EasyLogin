using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using EasyLogin.Domain.Enums;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;

public class LoginCommandHandler(
    IUserRepository userRepository,
    ITokenService tokenService,
    ITwoFactorService twoFactorService,
    IEmailService emailService,
    IEmailTemplateRenderer templateRenderer,
    IAuditLogger auditLogger)
    : IRequestHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var attempt = await userRepository.ValidateCredentialsAsync(request.Email, request.Password);

        if (!attempt.Success)
        {
            await auditLogger.WriteAsync(new AuditEntry
            {
                EventType = AuditEventType.LoginFailed,
                Success = false,
                ActorEmail = request.Email,
                FailureReason = attempt.FailureReason
            }, cancellationToken);

            throw new UnauthorizedAccessException(attempt.FailureReason);
        }

        ApplicationUser user = attempt.User!;
        IList<string> roles = attempt.Roles!;

        if (user.TwoFactorEnabled)
        {
            TwoFactorMethod method = user.TwoFactorMethod ?? TwoFactorMethod.Authenticator;
            TwoFactorChallengeTokenResult challengeToken = tokenService.GenerateTwoFactorChallengeToken(user);

            if (method == TwoFactorMethod.Email)
            {
                string code = await twoFactorService.GenerateEmailTwoFactorCodeAsync(user.Id);
                string body = await templateRenderer.RenderAsync("TwoFactorCode", new Dictionary<string, string>
                {
                    ["firstName"] = user.FirstName,
                    ["code"] = code,
                    ["minutes"] = "5"
                });
                await emailService.SendAsync(user.Email, "Your EasyLogin verification code", body);
            }

            await auditLogger.WriteAsync(new AuditEntry
            {
                EventType = AuditEventType.TwoFactorChallengeIssued,
                Success = true,
                ActorUserId = user.Id,
                ActorEmail = user.Email,
                Jti = challengeToken.Jti
            }, cancellationToken);

            return new AuthResponse(
                null,
                null,
                challengeToken.ExpiresIn,
                RequiresTwoFactor: true,
                TwoFactorToken: challengeToken.Token,
                TwoFactorMethod: method.ToDto());
        }

        AccessTokenResult accessToken = tokenService.GenerateAccessToken(user, roles);
        string rawRefreshToken = tokenService.GenerateRefreshToken();
        DateTimeOffset expiry = DateTimeOffset.UtcNow.AddDays(7);

        await userRepository.StoreRefreshTokenAsync(user.Id, HashHelper.Sha256(rawRefreshToken), expiry);

        await auditLogger.WriteAsync(new AuditEntry
        {
            EventType = AuditEventType.LoginSuccess,
            Success = true,
            ActorUserId = user.Id,
            ActorEmail = user.Email,
            Jti = accessToken.Jti
        }, cancellationToken);

        return new AuthResponse(accessToken.Token, rawRefreshToken, tokenService.AccessTokenExpiryMinutes * 60);
    }
}
