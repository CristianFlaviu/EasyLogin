using System.Security.Claims;
using EasyLogin.Domain.Entities;

namespace EasyLogin.Application.Interfaces;

public interface ITokenService
{
    AccessTokenResult GenerateAccessToken(ApplicationUser user, IList<string> roles);
    string GenerateRefreshToken();
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    int AccessTokenExpiryMinutes { get; }
}

public record AccessTokenResult(string Token, string Jti);
