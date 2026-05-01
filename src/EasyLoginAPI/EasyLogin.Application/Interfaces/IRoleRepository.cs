using EasyLogin.Application.Auth.Dtos;

namespace EasyLogin.Application.Interfaces;

public interface IRoleRepository
{
    Task<IList<RoleResponse>> GetAllRolesAsync();
    Task<RoleResponse> CreateRoleAsync(string name, string? description);
    Task DeleteRoleAsync(string roleId);
    Task<bool> RoleExistsAsync(string name);
}
