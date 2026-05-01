using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Common;
using EasyLogin.Domain.Entities;

namespace EasyLogin.Application.Interfaces;

public interface IUserRepository
{
    Task<(ApplicationUser User, IList<string> Roles)> ValidateCredentialsAsync(string email, string password);
    Task<ApplicationUser> CreateUserAsync(string firstName, string lastName, string email, string password);
    Task AssignRoleAsync(string userId, string roleName);
    Task<bool> EmailExistsAsync(string email);
    Task<ApplicationUser> GetByIdAsync(string userId);
    Task<(ApplicationUser User, IList<string> Roles)> GetByIdWithRolesAsync(string userId);
    Task StoreRefreshTokenAsync(string userId, string tokenHash, DateTimeOffset expiry);
    Task RevokeRefreshTokenAsync(string userId);
    Task<string> GeneratePasswordResetTokenAsync(string email);
    Task ResetPasswordAsync(string email, string token, string newPassword);
    Task<PaginatedList<UserListItemResponse>> GetPagedUsersAsync(int pageNumber, int pageSize);
    Task UpdateUserAsync(string userId, string firstName, string lastName, string email, bool isActive, IList<string> roles, string? newPassword);
    Task DeleteUserAsync(string userId);
}
