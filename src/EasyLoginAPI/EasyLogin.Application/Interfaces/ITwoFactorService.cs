using EasyLogin.Application.Auth.Dtos;

namespace EasyLogin.Application.Interfaces;

public interface ITwoFactorService
{
    Task<TwoFactorSetupResponse> BeginSetupAsync(string userId, string issuer);
    Task<bool> VerifyAuthenticatorCodeAsync(string userId, string code);
    Task<string> GenerateEmailTwoFactorCodeAsync(string userId);
    Task<bool> VerifyEmailTwoFactorCodeAsync(string userId, string code);
    Task SetTwoFactorEnabledAsync(string userId, bool enabled);
    Task SetTwoFactorMethodAsync(string userId, EasyLogin.Domain.Entities.TwoFactorMethod? method);
    Task ResetAuthenticatorAsync(string userId);
    Task<bool> CheckPasswordAsync(string userId, string password);
    Task<bool> IsLockedOutAsync(string userId);
    Task AccessFailedAsync(string userId);
    Task ResetAccessFailedCountAsync(string userId);
}
