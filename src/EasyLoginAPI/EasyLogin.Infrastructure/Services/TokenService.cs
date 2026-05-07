using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace EasyLogin.Infrastructure.Services;

public class TokenService(IConfiguration config) : ITokenService
{
    private const string TwoFactorTokenType = "2fa";

    private readonly string _key = config["Jwt:Key"]
        ?? throw new InvalidOperationException("Jwt:Key is not configured.");
    private readonly string _issuer = config["Jwt:Issuer"] ?? "EasyLogin";
    private readonly string _audience = config["Jwt:Audience"] ?? "EasyLogin";

    public int AccessTokenExpiryMinutes =>
        int.TryParse(config["Jwt:AccessTokenExpiryMinutes"], out var minutes) ? minutes : 15;

    private int TwoFactorChallengeExpiryMinutes =>
        int.TryParse(config["Jwt:TwoFactorChallengeExpiryMinutes"], out var minutes) ? minutes : 5;

    public AccessTokenResult GenerateAccessToken(ApplicationUser user, IList<string> roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jti = Guid.NewGuid().ToString();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, jti),
            new("firstName", user.FirstName),
            new("lastName", user.LastName),
        };

        if (user.TenantId.HasValue)
            claims.Add(new Claim("tenant_id", user.TenantId.Value.ToString()));

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(AccessTokenExpiryMinutes),
            signingCredentials: creds);

        return new AccessTokenResult(new JwtSecurityTokenHandler().WriteToken(token), jti);
    }

    public TwoFactorChallengeTokenResult GenerateTwoFactorChallengeToken(ApplicationUser user)
    {
        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(_key));
        SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha256);

        string jti = Guid.NewGuid().ToString();
        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, jti),
            new("token_type", TwoFactorTokenType)
        ];

        JwtSecurityToken token = new(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(TwoFactorChallengeExpiryMinutes),
            signingCredentials: creds);

        return new TwoFactorChallengeTokenResult(
            new JwtSecurityTokenHandler().WriteToken(token),
            jti,
            TwoFactorChallengeExpiryMinutes * 60);
    }

    public string GenerateRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));

        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateLifetime = false
        };

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, validationParams, out var securityToken);

        if (securityToken is not JwtSecurityToken jwtToken ||
            !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token algorithm.");
        }

        return principal;
    }

    public TwoFactorChallengePrincipal ValidateTwoFactorChallengeToken(string token)
    {
        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(_key));

        TokenValidationParameters validationParams = new()
        {
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        JwtSecurityTokenHandler handler = new();
        ClaimsPrincipal principal = handler.ValidateToken(token, validationParams, out SecurityToken securityToken);

        if (securityToken is not JwtSecurityToken jwtToken ||
            !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token algorithm.");
        }

        string? tokenType = principal.FindFirst("token_type")?.Value;
        if (tokenType != TwoFactorTokenType)
            throw new SecurityTokenException("Invalid token type.");

        string? userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? principal.FindFirst("sub")?.Value;
        string? email = principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value
            ?? principal.FindFirst(ClaimTypes.Email)?.Value
            ?? principal.FindFirst("email")?.Value;
        string? jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value
            ?? principal.FindFirst("jti")?.Value;

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(jti))
            throw new SecurityTokenException("Missing required 2FA challenge claims.");

        return new TwoFactorChallengePrincipal(userId, email, jti);
    }
}
