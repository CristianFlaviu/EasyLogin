using EasyLogin.Application.Auth.Dtos;
using EasyLogin.Application.Interfaces;
using EasyLogin.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EasyLogin.Infrastructure.Persistence;

public class RoleRepository(RoleManager<AppIdentityRole> roleManager) : IRoleRepository
{
    public async Task<IList<RoleResponse>> GetAllRolesAsync()
    {
        var roles = await roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
        return roles.Select(r => new RoleResponse(r.Id, r.Name ?? string.Empty, r.Description, r.IsSystemRole, r.CreatedAt, r.UpdatedAt)).ToList();
    }

    public async Task<RoleResponse> CreateRoleAsync(string name, string? description)
    {
        if (await roleManager.RoleExistsAsync(name))
            throw new InvalidOperationException($"Role '{name}' already exists.");

        var role = new AppIdentityRole
        {
            Name = name,
            Description = description,
            IsSystemRole = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = null
        };

        var result = await roleManager.CreateAsync(role);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        return new RoleResponse(role.Id, role.Name!, role.Description, role.IsSystemRole, role.CreatedAt, role.UpdatedAt);
    }

    public async Task DeleteRoleAsync(string roleId)
    {
        var role = await roleManager.FindByIdAsync(roleId)
            ?? throw new KeyNotFoundException($"Role {roleId} not found.");

        if (role.IsSystemRole)
            throw new InvalidOperationException($"Cannot delete system role '{role.Name}'.");

        var result = await roleManager.DeleteAsync(role);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    public Task<bool> RoleExistsAsync(string name)
        => roleManager.RoleExistsAsync(name);
}
