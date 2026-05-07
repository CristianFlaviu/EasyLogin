using System.Text.Encodings.Web;
using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using EasyLogin.Domain.Enums;
using EasyLogin.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace EasyLogin.Infrastructure.Services;

public class TwoFactorService(UserManager<AppIdentityUser> userManager) : ITwoFactorService
{
    public async Task<TwoFactorSetupResponse> BeginSetupAsync(string userId, string issuer)
    {
        AppIdentityUser user = await GetUserAsync(userId);
        string? key = await userManager.GetAuthenticatorKeyAsync(user);

        if (string.IsNullOrWhiteSpace(key))
        {
            IdentityResult resetResult = await userManager.ResetAuthenticatorKeyAsync(user);
            if (!resetResult.Succeeded)
                throw new InvalidOperationException(string.Join(", ", resetResult.Errors.Select(e => e.Description)));

            key = await userManager.GetAuthenticatorKeyAsync(user);
        }

        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Unable to create authenticator key.");

        string email = user.Email ?? user.UserName ?? user.Id;
        string otpAuthUri = GenerateOtpAuthUri(email, key, issuer);
        return new TwoFactorSetupResponse(otpAuthUri, FormatSharedKey(key));
    }

    public async Task<bool> VerifyAuthenticatorCodeAsync(string userId, string code)
    {
        AppIdentityUser user = await GetUserAsync(userId);
        string normalizedCode = NormalizeCode(code);
        return await userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, normalizedCode);
    }

    public async Task<string> GenerateEmailTwoFactorCodeAsync(string userId)
    {
        AppIdentityUser user = await GetUserAsync(userId);
        return await userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
    }

    public async Task<bool> VerifyEmailTwoFactorCodeAsync(string userId, string code)
    {
        AppIdentityUser user = await GetUserAsync(userId);
        string normalizedCode = NormalizeCode(code);
        return await userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, normalizedCode);
    }

    public async Task SetTwoFactorEnabledAsync(string userId, bool enabled)
    {
        AppIdentityUser user = await GetUserAsync(userId);
        IdentityResult result = await userManager.SetTwoFactorEnabledAsync(user, enabled);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    public async Task SetTwoFactorMethodAsync(string userId, TwoFactorMethod? method)
    {
        AppIdentityUser user = await GetUserAsync(userId);
        user.TwoFactorMethod = method;
        IdentityResult result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    public async Task ResetAuthenticatorAsync(string userId)
    {
        AppIdentityUser user = await GetUserAsync(userId);
        IdentityResult result = await userManager.ResetAuthenticatorKeyAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    public async Task<bool> CheckPasswordAsync(string userId, string password)
    {
        AppIdentityUser user = await GetUserAsync(userId);
        return await userManager.CheckPasswordAsync(user, password);
    }

    public async Task<bool> IsLockedOutAsync(string userId)
    {
        AppIdentityUser user = await GetUserAsync(userId);
        return await userManager.IsLockedOutAsync(user);
    }

    public async Task AccessFailedAsync(string userId)
    {
        AppIdentityUser user = await GetUserAsync(userId);
        await userManager.AccessFailedAsync(user);
    }

    public async Task ResetAccessFailedCountAsync(string userId)
    {
        AppIdentityUser user = await GetUserAsync(userId);
        await userManager.ResetAccessFailedCountAsync(user);
    }

    private async Task<AppIdentityUser> GetUserAsync(string userId)
        => await userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

    private string GenerateOtpAuthUri(string email, string key, string issuer)
    {
        string encodedIssuer = UrlEncoder.Default.Encode(issuer);
        string encodedEmail = UrlEncoder.Default.Encode(email);
        return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={key}&issuer={encodedIssuer}&digits=6";
    }

    private static string NormalizeCode(string code)
        => code.Replace(" ", string.Empty).Replace("-", string.Empty);

    private static string FormatSharedKey(string key)
        => string.Join(" ", Enumerable.Range(0, (key.Length + 3) / 4)
            .Select(i => key.Substring(i * 4, Math.Min(4, key.Length - i * 4))))
            .ToUpperInvariant();
}
