using EasyLogin.Application.Companies.Dtos;

namespace EasyLogin.Application.Interfaces;

public interface ICompanyRoleRepository
{
    Task<IList<CompanyRoleResponse>> GetByCompanyIdAsync(Guid companyId);
    Task<CompanyRoleResponse> CreateAsync(string name, string? description, Guid companyId);
    Task DeleteAsync(Guid id, Guid requiredCompanyId);
    Task<IList<string>> GetUserRoleNamesAsync(string userId);
    Task UpdateUserRolesAsync(string userId, IList<Guid> companyRoleIds, Guid callerCompanyId);
}
