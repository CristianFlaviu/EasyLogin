using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record RefreshTokenCommand(string AccessToken, string RefreshToken) : IRequest<AuthResponse>;

public class RefreshTokenCommandHandler(
    IUserRepository userRepository,
    ITokenService tokenService,
    IAuditLogger auditLogger)
    : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        string? userIdFromToken = null;
        try
        {
            var principal = tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
            userIdFromToken = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? principal.FindFirst("sub")?.Value;
        }
        catch
        {
            await auditLogger.WriteAsync(new AuditEntry
            {
                EventType = AuditEventType.RefreshTokenFailed,
                Success = false,
                FailureReason = "InvalidAccessToken"
            }, cancellationToken);
            throw new UnauthorizedAccessException();
        }

        if (userIdFromToken is null)
        {
            await auditLogger.WriteAsync(new AuditEntry
            {
                EventType = AuditEventType.RefreshTokenFailed,
                Success = false,
                FailureReason = "MissingSubjectClaim"
            }, cancellationToken);
            throw new UnauthorizedAccessException();
        }

        var user = await userRepository.GetByIdAsync(userIdFromToken);

        if (user.RefreshTokenHash is null ||
            user.RefreshTokenExpiry is null ||
            user.RefreshTokenExpiry <= DateTimeOffset.UtcNow ||
            user.RefreshTokenHash != HashHelper.Sha256(request.RefreshToken))
        {
            await auditLogger.WriteAsync(new AuditEntry
            {
                EventType = AuditEventType.RefreshTokenFailed,
                Success = false,
                ActorUserId = user.Id,
                ActorEmail = user.Email,
                FailureReason = "InvalidOrExpiredRefreshToken"
            }, cancellationToken);
            throw new UnauthorizedAccessException();
        }

        var (_, roles, _) = await userRepository.GetByIdWithRolesAsync(userIdFromToken);
        var newAccessToken = tokenService.GenerateAccessToken(user, roles);
        var newRawRefreshToken = tokenService.GenerateRefreshToken();
        var expiry = DateTimeOffset.UtcNow.AddDays(7);

        await userRepository.StoreRefreshTokenAsync(user.Id, HashHelper.Sha256(newRawRefreshToken), expiry);

        await auditLogger.WriteAsync(new AuditEntry
        {
            EventType = AuditEventType.RefreshToken,
            Success = true,
            ActorUserId = user.Id,
            ActorEmail = user.Email,
            Jti = newAccessToken.Jti
        }, cancellationToken);

        return new AuthResponse(newAccessToken.Token, newRawRefreshToken, tokenService.AccessTokenExpiryMinutes * 60);
    }
}
