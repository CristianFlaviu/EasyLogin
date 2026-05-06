using EasyLogin.Application.Tenants.Dtos;

namespace EasyLogin.Application.Interfaces;

public interface ITenantRoleRepository
{
    Task<IList<TenantRoleResponse>> GetByTenantIdAsync(Guid tenantId);
    Task<TenantRoleResponse> CreateAsync(string name, string? description, Guid tenantId);
    Task DeleteAsync(Guid id, Guid requiredTenantId);
    Task<IList<string>> GetUserRoleNamesAsync(string userId);
    Task UpdateUserRolesAsync(string userId, IList<Guid> tenantRoleIds, Guid callerTenantId);
}
