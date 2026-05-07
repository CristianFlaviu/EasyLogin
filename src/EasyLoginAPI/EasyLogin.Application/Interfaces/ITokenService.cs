using System.Security.Claims;
using EasyLogin.Domain.Entities;

namespace EasyLogin.Application.Interfaces;

public interface ITokenService
{
    AccessTokenResult GenerateAccessToken(ApplicationUser user, IList<string> roles);
    TwoFactorChallengeTokenResult GenerateTwoFactorChallengeToken(ApplicationUser user);
    string GenerateRefreshToken();
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    TwoFactorChallengePrincipal ValidateTwoFactorChallengeToken(string token);
    int AccessTokenExpiryMinutes { get; }
}

public record AccessTokenResult(string Token, string Jti);

public record TwoFactorChallengeTokenResult(string Token, string Jti, int ExpiresIn);

public record TwoFactorChallengePrincipal(string UserId, string Email, string Jti);
