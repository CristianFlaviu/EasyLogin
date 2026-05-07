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

        if (identityUser.Status != UserStatus.Active)
            return LoginAttemptResult.Failed("UserInactive");

        if (!identityUser.EmailConfirmed)
            return LoginAttemptResult.Failed("EmailNotConfirmed");

        if (await userManager.IsLockedOutAsync(identityUser))
            return LoginAttemptResult.Failed("LockedOut");

        if (!await userManager.CheckPasswordAsync(identityUser, password))
        {
            await userManager.AccessFailedAsync(identityUser);
            return await userManager.IsLockedOutAsync(identityUser)
                ? LoginAttemptResult.Failed("LockedOut")
                : LoginAttemptResult.Failed("InvalidPassword");
        }

        if (!identityUser.TwoFactorEnabled)
            await userManager.ResetAccessFailedCountAsync(identityUser);

        var roles = await userManager.GetRolesAsync(identityUser);
        var user = await MapWithTenantAsync(identityUser);
        return LoginAttemptResult.Ok(user, roles);
    }

    public async Task<ApplicationUser> CreateUserAsync(string firstName, string lastName, string email, string password, Guid? tenantId = null, bool emailConfirmed = true)
    {
        var identityUser = new AppIdentityUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = emailConfirmed,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = null,
            TenantId = tenantId
        };

        var result = await userManager.CreateAsync(identityUser, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        return await MapWithTenantAsync(identityUser);
    }

    public async Task<ApplicationUser?> GetByEmailAsync(string email)
    {
        AppIdentityUser? user = await userManager.FindByEmailAsync(email);
        return user is null ? null : await MapWithTenantAsync(user);
    }

    public async Task<ApplicationUser> CreatePendingUserAsync(string firstName, string lastName, string email, Guid? tenantId)
    {
        AppIdentityUser identityUser = new AppIdentityUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = true,
            Status = UserStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = null,
            TenantId = tenantId
        };

        IdentityResult result = await userManager.CreateAsync(identityUser);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        return await MapWithTenantAsync(identityUser);
    }

    public async Task AssignRoleAsync(string userId, string roleName)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        await userManager.AddToRoleAsync(user, roleName);
    }

    public async Task<bool> EmailExistsAsync(string email)
        => await userManager.FindByEmailAsync(email) is not null;

    public async Task<bool> IsInTenantAsync(string userId, Guid tenantId)
    {
        AppIdentityUser user = await userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        return await IsInTenantInternalAsync(user, tenantId);
    }

    public async Task<ApplicationUser> GetByIdAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");
        return await MapWithTenantAsync(user);
    }

    public async Task<(ApplicationUser User, IList<string> SystemRoles, IList<string> TenantRoles)> GetByIdWithRolesAsync(
        string userId, Guid? requiredTenantId = null)
    {
        var identityUser = await userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        if (requiredTenantId.HasValue
            && !await IsInTenantInternalAsync(identityUser, requiredTenantId.Value))
        {
            throw new UnauthorizedAccessException();
        }

        var user = await MapWithTenantAsync(identityUser);
        var systemRoles = await userManager.GetRolesAsync(identityUser);
        var tenantRoles = await GetTenantRoleNamesAsync(userId, requiredTenantId);

        return (user, systemRoles, tenantRoles);
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

    public async Task StoreInviteTokenAsync(string userId, string tokenHash, DateTimeOffset expiresAt)
    {
        AppIdentityUser user = await userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        DateTimeOffset now = DateTimeOffset.UtcNow;
        await RevokeInviteTokensInternalAsync(user.Id, now);

        db.InviteTokens.Add(new InviteToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = now,
            IsUsed = false,
            UsedAt = null
        });

        await db.SaveChangesAsync();
    }

    public async Task RevokeInviteTokensAsync(string userId)
    {
        AppIdentityUser user = await userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        await RevokeInviteTokensInternalAsync(user.Id, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();
    }

    public async Task<(string UserId, string Email, string FirstName, string LastName)> ValidateInviteTokenAsync(string tokenHash)
    {
        InviteToken invite = await db.InviteTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash)
            ?? throw new KeyNotFoundException("Invite token was not found.");

        AppIdentityUser user = await userManager.FindByIdAsync(invite.UserId)
            ?? throw new KeyNotFoundException($"User {invite.UserId} not found.");

        if (invite.IsUsed)
            throw new InviteTokenUsedException("This invite link has already been used.");

        if (invite.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            if (user.Status == UserStatus.Pending)
            {
                user.Status = UserStatus.Expired;
                user.UpdatedAt = DateTimeOffset.UtcNow;
                await userManager.UpdateAsync(user);
            }

            throw new InviteTokenExpiredException("This invite link has expired.");
        }

        if (user.Status != UserStatus.Pending && user.Status != UserStatus.Active)
            throw new InvalidOperationException("This invite is no longer pending.");

        return (user.Id, user.Email ?? string.Empty, user.FirstName, user.LastName);
    }

    public async Task<(string UserId, string Email)> AcceptInviteAsync(string tokenHash, string newPassword)
    {
        InviteToken invite = await db.InviteTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash)
            ?? throw new KeyNotFoundException("Invite token was not found.");

        AppIdentityUser user = await userManager.FindByIdAsync(invite.UserId)
            ?? throw new KeyNotFoundException($"User {invite.UserId} not found.");

        if (invite.IsUsed)
            throw new InviteTokenUsedException("This invite link has already been used.");

        if (invite.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            if (user.Status == UserStatus.Pending)
            {
                user.Status = UserStatus.Expired;
                user.UpdatedAt = DateTimeOffset.UtcNow;
                await userManager.UpdateAsync(user);
            }

            throw new InviteTokenExpiredException("This invite link has expired.");
        }

        if (user.Status != UserStatus.Pending && user.Status != UserStatus.Active)
            throw new InvalidOperationException("This invite is no longer pending.");

        string resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        IdentityResult passwordResult = await userManager.ResetPasswordAsync(user, resetToken, newPassword);
        if (!passwordResult.Succeeded)
            throw new InvalidOperationException(string.Join(", ", passwordResult.Errors.Select(e => e.Description)));

        if (user.Status == UserStatus.Pending)
        {
            user.Status = UserStatus.Active;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            IdentityResult updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                throw new InvalidOperationException(string.Join(", ", updateResult.Errors.Select(e => e.Description)));
        }

        invite.IsUsed = true;
        invite.UsedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return (user.Id, user.Email ?? string.Empty);
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

    public async Task<string> GenerateEmailConfirmationTokenAsync(string email)
    {
        AppIdentityUser user = await userManager.FindByEmailAsync(email)
            ?? throw new KeyNotFoundException("No account found with that email address.");

        return await userManager.GenerateEmailConfirmationTokenAsync(user);
    }

    public async Task ConfirmEmailAsync(string email, string token)
    {
        AppIdentityUser user = await userManager.FindByEmailAsync(email)
            ?? throw new KeyNotFoundException("No account found with that email address.");

        IdentityResult result = await userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    public async Task<PaginatedList<UserListItemResponse>> GetPagedUsersAsync(int pageNumber, int pageSize, Guid? tenantId = null)
    {
        var query = from u in db.Users
                    join c in db.Tenants on u.TenantId equals c.Id into gc
                    from c in gc.DefaultIfEmpty()
                    where tenantId == null
                        || u.TenantId == tenantId
                        || db.UserTenantRoles
                            .Where(ucr => ucr.UserId == u.Id)
                            .Join(db.TenantRoles, ucr => ucr.TenantRoleId, cr => cr.Id, (_, cr) => cr.TenantId)
                            .Any(tid => tid == tenantId!.Value)
                    orderby u.LastName, u.FirstName
                    select new { u, TenantName = (string?)c.Name };

        var total = await query.CountAsync();
        var page = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        var userIds = page.Select(x => x.u.Id).ToList();
        var tenantRolesMap = await db.UserTenantRoles
            .Where(ucr => userIds.Contains(ucr.UserId))
            .Join(db.TenantRoles, ucr => ucr.TenantRoleId, cr => cr.Id, (ucr, cr) => new { ucr.UserId, cr.Name })
            .ToListAsync();
        var tenantRolesByUser = tenantRolesMap
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => (IList<string>)g.Select(x => x.Name).ToList());

        var items = new List<UserListItemResponse>();
        foreach (var x in page)
        {
            var systemRoles = await userManager.GetRolesAsync(x.u);
            tenantRolesByUser.TryGetValue(x.u.Id, out var cRoles);
            items.Add(new UserListItemResponse(
                x.u.Id, x.u.FirstName, x.u.LastName, x.u.Email ?? string.Empty,
                x.u.IsActive, x.u.CreatedAt, x.u.UpdatedAt,
                x.u.TenantId, x.TenantName,
                systemRoles, cRoles ?? [],
                x.u.Status.ToString()));
        }

        return new PaginatedList<UserListItemResponse>(items, total, pageNumber, pageSize);
    }

    public async Task UpdateUserAsync(
        string userId, string firstName, string lastName, string email,
        bool isActive, IList<string>? systemRoles, string? newPassword,
        Guid? requiredTenantId = null)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        if (requiredTenantId.HasValue
            && !await IsInTenantInternalAsync(user, requiredTenantId.Value))
        {
            throw new UnauthorizedAccessException();
        }

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
        user.Status = isActive ? UserStatus.Active : UserStatus.Suspended;
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

    public async Task DeleteUserAsync(string userId, Guid? requiredTenantId = null)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        if (requiredTenantId.HasValue
            && !await IsInTenantInternalAsync(user, requiredTenantId.Value))
        {
            throw new UnauthorizedAccessException();
        }

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    private async Task<ApplicationUser> MapWithTenantAsync(AppIdentityUser identityUser)
    {
        var user = identityUser.Adapt<ApplicationUser>();
        if (identityUser.TenantId.HasValue)
        {
            var tenant = await db.Tenants.FindAsync(identityUser.TenantId.Value);
            user.TenantName = tenant?.Name;
        }
        return user;
    }

    private async Task<IList<string>> GetTenantRoleNamesAsync(string userId, Guid? tenantId = null)
    {
        var query = db.UserTenantRoles
            .Where(ucr => ucr.UserId == userId)
            .Join(db.TenantRoles, ucr => ucr.TenantRoleId, cr => cr.Id, (_, cr) => cr);

        if (tenantId.HasValue)
            query = query.Where(cr => cr.TenantId == tenantId.Value);

        return await query
            .Select(cr => cr.Name)
            .ToListAsync();
    }

    private async Task<bool> IsInTenantInternalAsync(AppIdentityUser user, Guid tenantId)
    {
        if (user.TenantId == tenantId)
            return true;

        return await db.UserTenantRoles
            .Where(ucr => ucr.UserId == user.Id)
            .Join(db.TenantRoles, ucr => ucr.TenantRoleId, cr => cr.Id, (_, cr) => cr.TenantId)
            .AnyAsync(tid => tid == tenantId);
    }

    private async Task RevokeInviteTokensInternalAsync(string userId, DateTimeOffset now)
    {
        List<InviteToken> activeTokens = await db.InviteTokens
            .Where(t => t.UserId == userId && !t.IsUsed)
            .ToListAsync();

        foreach (InviteToken token in activeTokens)
        {
            token.IsUsed = true;
            token.UsedAt = now;
        }
    }
}
