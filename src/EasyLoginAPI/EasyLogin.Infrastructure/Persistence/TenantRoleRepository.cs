using EasyLogin.Application.Tenants.Dtos;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyLogin.Infrastructure.Persistence;

public class TenantRoleRepository(AppDbContext db) : ITenantRoleRepository
{
    public async Task<IList<TenantRoleResponse>> GetByTenantIdAsync(Guid tenantId)
    {
        var roles = await db.TenantRoles
            .Where(cr => cr.TenantId == tenantId)
            .OrderBy(cr => cr.Name)
            .ToListAsync();
        return roles.Select(Map).ToList();
    }

    public async Task<TenantRoleResponse> CreateAsync(string name, string? description, Guid tenantId)
    {
        var exists = await db.TenantRoles.AnyAsync(cr => cr.Name == name && cr.TenantId == tenantId);
        if (exists)
            throw new InvalidOperationException($"Role '{name}' already exists in this tenant.");

        var role = new TenantRole
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            TenantId = tenantId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = null
        };
        db.TenantRoles.Add(role);
        await db.SaveChangesAsync();
        return Map(role);
    }

    public async Task DeleteAsync(Guid id, Guid requiredTenantId)
    {
        var role = await db.TenantRoles.FindAsync(id)
            ?? throw new KeyNotFoundException($"Role {id} not found.");

        if (role.TenantId != requiredTenantId)
            throw new UnauthorizedAccessException();

        var inUse = await db.UserTenantRoles.AnyAsync(ucr => ucr.TenantRoleId == id);
        if (inUse)
            throw new InvalidOperationException($"Role '{role.Name}' is assigned to one or more users and cannot be deleted.");

        db.TenantRoles.Remove(role);
        await db.SaveChangesAsync();
    }

    public async Task<IList<string>> GetUserRoleNamesAsync(string userId)
        => await db.UserTenantRoles
            .Where(ucr => ucr.UserId == userId)
            .Join(db.TenantRoles, ucr => ucr.TenantRoleId, cr => cr.Id, (_, cr) => cr.Name)
            .ToListAsync();

    public async Task UpdateUserRolesAsync(string userId, IList<Guid> tenantRoleIds, Guid callerTenantId)
    {
        if (tenantRoleIds.Count > 0)
        {
            var validIds = await db.TenantRoles
                .Where(cr => tenantRoleIds.Contains(cr.Id) && cr.TenantId == callerTenantId)
                .Select(cr => cr.Id)
                .ToListAsync();

            var invalid = tenantRoleIds.Except(validIds).ToList();
            if (invalid.Count > 0)
                throw new InvalidOperationException("One or more roles do not belong to this tenant.");
        }

        var existing = await db.UserTenantRoles
            .Where(ucr => ucr.UserId == userId)
            .Join(db.TenantRoles, ucr => ucr.TenantRoleId, cr => cr.Id,
                (ucr, cr) => new { ucr, cr.TenantId })
            .Where(x => x.TenantId == callerTenantId)
            .Select(x => x.ucr)
            .ToListAsync();

        db.UserTenantRoles.RemoveRange(existing);

        foreach (var roleId in tenantRoleIds)
            db.UserTenantRoles.Add(new UserTenantRole { UserId = userId, TenantRoleId = roleId });


        await db.SaveChangesAsync();
    }

    private static TenantRoleResponse Map(TenantRole cr)
        => new(cr.Id, cr.Name, cr.Description, cr.TenantId, cr.CreatedAt, cr.UpdatedAt);
}
