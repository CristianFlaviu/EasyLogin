using EasyLogin.Application.Companies.Dtos;

namespace EasyLogin.Application.Interfaces;

public interface ICompanyRepository
{
    Task<IList<CompanyResponse>> GetAllAsync();
    Task<CompanyResponse> GetByIdAsync(Guid id);
    Task<CompanyResponse> CreateAsync(string name);
    Task<CompanyResponse> UpdateAsync(Guid id, string name, bool isActive);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}
