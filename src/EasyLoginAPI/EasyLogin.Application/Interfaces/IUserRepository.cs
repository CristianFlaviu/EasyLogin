using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Common;
using EasyLogin.Domain.Entities;
using EasyLogin.Domain.Enums;

namespace EasyLogin.Application.Interfaces;

public interface IUserRepository
{
    Task<LoginAttemptResult> ValidateCredentialsAsync(string email, string password);
    Task<ApplicationUser> CreateUserAsync(string firstName, string lastName, string email, string password, Guid? tenantId = null, bool emailConfirmed = true);
    Task<ApplicationUser?> GetByEmailAsync(string email);
    Task<ApplicationUser> CreatePendingUserAsync(string firstName, string lastName, string email, Guid? tenantId);
    Task AssignRoleAsync(string userId, string roleName);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> IsInTenantAsync(string userId, Guid tenantId);
    Task<ApplicationUser> GetByIdAsync(string userId);
    Task<(ApplicationUser User, IList<string> SystemRoles, IList<string> TenantRoles)> GetByIdWithRolesAsync(string userId, Guid? requiredTenantId = null);
    Task StoreRefreshTokenAsync(string userId, string tokenHash, DateTimeOffset expiry);
    Task RevokeRefreshTokenAsync(string userId);
    Task StoreInviteTokenAsync(string userId, string tokenHash, DateTimeOffset expiresAt);
    Task RevokeInviteTokensAsync(string userId);
    Task<(string UserId, string Email, string FirstName, string LastName)> ValidateInviteTokenAsync(string tokenHash);
    Task<(string UserId, string Email)> AcceptInviteAsync(string tokenHash, string newPassword);
    Task<string> GeneratePasswordResetTokenAsync(string email);
    Task ResetPasswordAsync(string email, string token, string newPassword);
    Task<string> GenerateEmailConfirmationTokenAsync(string email);
    Task ConfirmEmailAsync(string email, string token);
    Task<PaginatedList<UserListItemResponse>> GetPagedUsersAsync(int pageNumber, int pageSize, Guid? tenantId = null);
    Task UpdateUserAsync(string userId, string firstName, string lastName, string email, UserStatus status, IList<string>? systemRoles, string? newPassword, Guid? requiredTenantId = null);
    Task DeleteUserAsync(string userId, Guid? requiredTenantId = null);
}
