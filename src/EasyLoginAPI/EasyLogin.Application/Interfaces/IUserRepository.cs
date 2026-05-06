using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Common;
using EasyLogin.Domain.Entities;

namespace EasyLogin.Application.Interfaces;

public interface IUserRepository
{
    Task<LoginAttemptResult> ValidateCredentialsAsync(string email, string password);
    Task<ApplicationUser> CreateUserAsync(string firstName, string lastName, string email, string password, Guid? companyId = null);
    Task<ApplicationUser?> GetByEmailAsync(string email);
    Task<ApplicationUser> CreatePendingUserAsync(string firstName, string lastName, string email, Guid? companyId);
    Task AssignRoleAsync(string userId, string roleName);
    Task<bool> EmailExistsAsync(string email);
    Task<ApplicationUser> GetByIdAsync(string userId);
    Task<(ApplicationUser User, IList<string> SystemRoles, IList<string> CompanyRoles)> GetByIdWithRolesAsync(string userId, Guid? requiredCompanyId = null);
    Task StoreRefreshTokenAsync(string userId, string tokenHash, DateTimeOffset expiry);
    Task RevokeRefreshTokenAsync(string userId);
    Task StoreInviteTokenAsync(string userId, string tokenHash, DateTimeOffset expiresAt);
    Task<(string UserId, string Email, string FirstName, string LastName)> ValidateInviteTokenAsync(string tokenHash);
    Task<(string UserId, string Email)> AcceptInviteAsync(string tokenHash, string newPassword);
    Task<string> GeneratePasswordResetTokenAsync(string email);
    Task ResetPasswordAsync(string email, string token, string newPassword);
    Task<PaginatedList<UserListItemResponse>> GetPagedUsersAsync(int pageNumber, int pageSize, Guid? companyId = null);
    Task UpdateUserAsync(string userId, string firstName, string lastName, string email, bool isActive, IList<string>? systemRoles, string? newPassword, Guid? requiredCompanyId = null);
    Task DeleteUserAsync(string userId, Guid? requiredCompanyId = null);
}
