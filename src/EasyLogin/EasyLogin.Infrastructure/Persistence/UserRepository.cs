using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using EasyLogin.Infrastructure.Identity;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EasyLogin.Infrastructure.Persistence;

public class UserRepository(UserManager<AppIdentityUser> userManager) : IUserRepository
{
    public async Task<(ApplicationUser User, IList<string> Roles)> ValidateCredentialsAsync(string email, string password)
    {
        var identityUser = await userManager.FindByEmailAsync(email)
            ?? throw new UnauthorizedAccessException();

        if (!identityUser.IsActive)
            throw new UnauthorizedAccessException();

        var valid = await userManager.CheckPasswordAsync(identityUser, password);
        if (!valid)
            throw new UnauthorizedAccessException();

        var roles = await userManager.GetRolesAsync(identityUser);
        return (identityUser.Adapt<ApplicationUser>(), roles);
    }

    public async Task<ApplicationUser> CreateUserAsync(string firstName, string lastName, string email, string password)
    {
        var identityUser = new AppIdentityUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await userManager.CreateAsync(identityUser, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        return identityUser.Adapt<ApplicationUser>();
    }

    public async Task AssignRoleAsync(string userId, string roleName)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        await userManager.AddToRoleAsync(user, roleName);
    }

    public async Task<bool> EmailExistsAsync(string email)
        => await userManager.FindByEmailAsync(email) is not null;

    public async Task<ApplicationUser> GetByIdAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");
        return user.Adapt<ApplicationUser>();
    }

    public async Task<(ApplicationUser User, IList<string> Roles)> GetByIdWithRolesAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");
        var roles = await userManager.GetRolesAsync(user);
        return (user.Adapt<ApplicationUser>(), roles);
    }

    public async Task StoreRefreshTokenAsync(string userId, string tokenHash, DateTimeOffset expiry)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        user.RefreshTokenHash = tokenHash;
        user.RefreshTokenExpiry = expiry;
        await userManager.UpdateAsync(user);
    }

    public async Task RevokeRefreshTokenAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        user.RefreshTokenHash = null;
        user.RefreshTokenExpiry = null;
        await userManager.UpdateAsync(user);
    }

    public async Task<string> GeneratePasswordResetTokenAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email)
            ?? throw new KeyNotFoundException("No account found with that email address.");

        return await userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task ResetPasswordAsync(string email, string token, string newPassword)
    {
        var user = await userManager.FindByEmailAsync(email)
            ?? throw new KeyNotFoundException("No account found with that email address.");

        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    public async Task<PaginatedList<UserListItemResponse>> GetPagedUsersAsync(int pageNumber, int pageSize)
    {
        var query = userManager.Users.OrderBy(u => u.LastName).ThenBy(u => u.FirstName);
        var total = await query.CountAsync();

        var users = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = new List<UserListItemResponse>();
        foreach (var u in users)
        {
            var roles = await userManager.GetRolesAsync(u);
            items.Add(new UserListItemResponse(u.Id, u.FirstName, u.LastName, u.Email ?? string.Empty,
                u.IsActive, u.CreatedAt, roles));
        }

        return new PaginatedList<UserListItemResponse>(items, total, pageNumber, pageSize);
    }
}
