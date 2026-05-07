using EasyLogin.Application.Interfaces;

namespace EasyLogin.Application.Auth.Commands;

internal static class TwoFactorCommandHelpers
{
    public static async Task<bool> VerifyAuthenticatorCodeAsync(
        string userId,
        string? code,
        ITwoFactorService twoFactorService)
    {
        if (!string.IsNullOrWhiteSpace(code)
            && await twoFactorService.VerifyAuthenticatorCodeAsync(userId, code))
        {
            return true;
        }

        return false;
    }

    public static async Task<bool> VerifyEmailCodeAsync(
        string userId,
        string? code,
        ITwoFactorService twoFactorService)
    {
        if (!string.IsNullOrWhiteSpace(code)
            && await twoFactorService.VerifyEmailTwoFactorCodeAsync(userId, code))
        {
            return true;
        }

        return false;
    }
}
