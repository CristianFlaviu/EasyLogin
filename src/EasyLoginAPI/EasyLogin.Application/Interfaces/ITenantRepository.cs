using EasyLogin.Application.Tenants.Dtos;

namespace EasyLogin.Application.Interfaces;

public interface ITenantRepository
{
    Task<IList<TenantResponse>> GetAllAsync();
    Task<TenantResponse> GetByIdAsync(Guid id);
    Task<TenantResponse> CreateAsync(string name);
    Task<TenantResponse> UpdateAsync(Guid id, string name, bool isActive);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> IsActiveAsync(Guid id);
}
