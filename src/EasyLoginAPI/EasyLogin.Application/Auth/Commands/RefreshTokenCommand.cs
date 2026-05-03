using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using MediatR;

namespace EasyLogin.Application.Auth.Commands;

public record RefreshTokenCommand(string AccessToken, string RefreshToken) : IRequest<AuthResponse>;

public class RefreshTokenCommandHandler(IUserRepository userRepository, ITokenService tokenService)
    : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var principal = tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value;

        if (userId is null)
            throw new UnauthorizedAccessException();

        var user = await userRepository.GetByIdAsync(userId);

        if (user.RefreshTokenHash is null ||
            user.RefreshTokenExpiry is null ||
            user.RefreshTokenExpiry <= DateTimeOffset.UtcNow ||
            user.RefreshTokenHash != HashHelper.Sha256(request.RefreshToken))
        {
            throw new UnauthorizedAccessException();
        }

        var (_, roles, _) = await userRepository.GetByIdWithRolesAsync(userId);
        var newAccessToken = tokenService.GenerateAccessToken(user, roles);
        var newRawRefreshToken = tokenService.GenerateRefreshToken();
        var expiry = DateTimeOffset.UtcNow.AddDays(7);

        await userRepository.StoreRefreshTokenAsync(user.Id, HashHelper.Sha256(newRawRefreshToken), expiry);

        return new AuthResponse(newAccessToken, newRawRefreshToken, tokenService.AccessTokenExpiryMinutes * 60);
    }
}
