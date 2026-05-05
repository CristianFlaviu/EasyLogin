using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Common;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using EasyLogin.Infrastructure.Identity;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EasyLogin.Infrastructure.Persistence;

public class UserRepository(UserManager<AppIdentityUser> userManager, AppDbContext db) : IUserRepository
{
    public async Task<LoginAttemptResult> ValidateCredentialsAsync(string email, string password)
    {
        var identityUser = await userManager.FindByEmailAsync(email);
        if (identityUser is null)
            return LoginAttemptResult.Failed("UserNotFound");

        if (!identityUser.IsActive)
            return LoginAttemptResult.Failed("UserInactive");

        if (await userManager.IsLockedOutAsync(identityUser))
            return LoginAttemptResult.Failed("LockedOut");

        if (!await userManager.CheckPasswordAsync(identityUser, password))
        {
            await userManager.AccessFailedAsync(identityUser);
            return await userManager.IsLockedOutAsync(identityUser)
                ? LoginAttemptResult.Failed("LockedOut")
                : LoginAttemptResult.Failed("InvalidPassword");
        }

        await userManager.ResetAccessFailedCountAsync(identityUser);

        var roles = await userManager.GetRolesAsync(identityUser);
        var user = await MapWithCompanyAsync(identityUser);
        return LoginAttemptResult.Ok(user, roles);
    }

    public async Task<ApplicationUser> CreateUserAsync(string firstName, string lastName, string email, string password, Guid? companyId = null)
    {
        var identityUser = new AppIdentityUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = null,
            CompanyId = companyId
        };

        var result = await userManager.CreateAsync(identityUser, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        return await MapWithCompanyAsync(identityUser);
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
        return await MapWithCompanyAsync(user);
    }

    public async Task<(ApplicationUser User, IList<string> SystemRoles, IList<string> CompanyRoles)> GetByIdWithRolesAsync(
        string userId, Guid? requiredCompanyId = null)
    {
        var identityUser = await userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        if (requiredCompanyId.HasValue && identityUser.CompanyId != requiredCompanyId)
            throw new UnauthorizedAccessException();

        var user = await MapWithCompanyAsync(identityUser);
        var systemRoles = await userManager.GetRolesAsync(identityUser);
        var companyRoles = await GetCompanyRoleNamesAsync(userId);

        return (user, systemRoles, companyRoles);
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

    public async Task<PaginatedList<UserListItemResponse>> GetPagedUsersAsync(int pageNumber, int pageSize, Guid? companyId = null)
    {
        var query = from u in db.Users
                    join c in db.Companies on u.CompanyId equals c.Id into gc
                    from c in gc.DefaultIfEmpty()
                    where companyId == null || u.CompanyId == companyId
                    orderby u.LastName, u.FirstName
                    select new { u, CompanyName = (string?)c.Name };

        var total = await query.CountAsync();
        var page = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        var userIds = page.Select(x => x.u.Id).ToList();
        var companyRolesMap = await db.UserCompanyRoles
            .Where(ucr => userIds.Contains(ucr.UserId))
            .Join(db.CompanyRoles, ucr => ucr.CompanyRoleId, cr => cr.Id, (ucr, cr) => new { ucr.UserId, cr.Name })
            .ToListAsync();
        var companyRolesByUser = companyRolesMap
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => (IList<string>)g.Select(x => x.Name).ToList());

        var items = new List<UserListItemResponse>();
        foreach (var x in page)
        {
            var systemRoles = await userManager.GetRolesAsync(x.u);
            companyRolesByUser.TryGetValue(x.u.Id, out var cRoles);
            items.Add(new UserListItemResponse(
                x.u.Id, x.u.FirstName, x.u.LastName, x.u.Email ?? string.Empty,
                x.u.IsActive, x.u.CreatedAt, x.u.UpdatedAt,
                x.u.CompanyId, x.CompanyName,
                systemRoles, cRoles ?? []));
        }

        return new PaginatedList<UserListItemResponse>(items, total, pageNumber, pageSize);
    }

    public async Task UpdateUserAsync(
        string userId, string firstName, string lastName, string email,
        bool isActive, IList<string>? systemRoles, string? newPassword,
        Guid? requiredCompanyId = null)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        if (requiredCompanyId.HasValue && user.CompanyId != requiredCompanyId)
            throw new UnauthorizedAccessException();

        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await userManager.FindByEmailAsync(email);
            if (existing is not null && existing.Id != userId)
                throw new InvalidOperationException($"Email '{email}' is already in use.");

            user.Email = email;
            user.NormalizedEmail = email.ToUpperInvariant();
            user.UserName = email;
            user.NormalizedUserName = email.ToUpperInvariant();
        }

        user.FirstName = firstName;
        user.LastName = lastName;
        user.IsActive = isActive;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            throw new InvalidOperationException(string.Join(", ", updateResult.Errors.Select(e => e.Description)));

        if (systemRoles is not null)
        {
            var currentRoles = await userManager.GetRolesAsync(user);
            await userManager.RemoveFromRolesAsync(user, currentRoles);
            if (systemRoles.Count > 0)
                await userManager.AddToRolesAsync(user, systemRoles);
        }

        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var pwResult = await userManager.ResetPasswordAsync(user, resetToken, newPassword);
            if (!pwResult.Succeeded)
                throw new InvalidOperationException(string.Join(", ", pwResult.Errors.Select(e => e.Description)));
        }
    }

    public async Task DeleteUserAsync(string userId, Guid? requiredCompanyId = null)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        if (requiredCompanyId.HasValue && user.CompanyId != requiredCompanyId)
            throw new UnauthorizedAccessException();

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    private async Task<ApplicationUser> MapWithCompanyAsync(AppIdentityUser identityUser)
    {
        var user = identityUser.Adapt<ApplicationUser>();
        if (identityUser.CompanyId.HasValue)
        {
            var company = await db.Companies.FindAsync(identityUser.CompanyId.Value);
            user.CompanyName = company?.Name;
        }
        return user;
    }

    private async Task<IList<string>> GetCompanyRoleNamesAsync(string userId)
        => await db.UserCompanyRoles
            .Where(ucr => ucr.UserId == userId)
            .Join(db.CompanyRoles, ucr => ucr.CompanyRoleId, cr => cr.Id, (_, cr) => cr.Name)
            .ToListAsync();
}
