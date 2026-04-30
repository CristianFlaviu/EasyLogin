using System.Security.Claims;
using EasyLogin.Domain.Entities;

namespace EasyLogin.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user, IList<string> roles);
    string GenerateRefreshToken();
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    int AccessTokenExpiryMinutes { get; }
}
