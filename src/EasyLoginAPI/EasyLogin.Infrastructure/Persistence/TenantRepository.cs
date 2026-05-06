using EasyLogin.Application.Tenants.Dtos;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyLogin.Infrastructure.Persistence;

public class TenantRepository(AppDbContext db) : ITenantRepository
{
    public async Task<IList<TenantResponse>> GetAllAsync()
    {
        var tenants = await db.Tenants.OrderBy(c => c.Name).ToListAsync();
        return tenants.Select(Map).ToList();
    }

    public async Task<TenantResponse> GetByIdAsync(Guid id)
    {
        var tenant = await db.Tenants.FindAsync(id)
            ?? throw new KeyNotFoundException($"Tenant {id} not found.");
        return Map(tenant);
    }

    public async Task<TenantResponse> CreateAsync(string name)
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = null
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        return Map(tenant);
    }

    public async Task<TenantResponse> UpdateAsync(Guid id, string name, bool isActive)
    {
        var tenant = await db.Tenants.FindAsync(id)
            ?? throw new KeyNotFoundException($"Tenant {id} not found.");
        tenant.Name = name;
        tenant.IsActive = isActive;
        tenant.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return Map(tenant);
    }

    public async Task DeleteAsync(Guid id)
    {
        var tenant = await db.Tenants.FindAsync(id)
            ?? throw new KeyNotFoundException($"Tenant {id} not found.");
        db.Tenants.Remove(tenant);
        await db.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(Guid id)
        => await db.Tenants.AnyAsync(c => c.Id == id);

    private static TenantResponse Map(Tenant c)
        => new(c.Id, c.Name, c.IsActive, c.CreatedAt, c.UpdatedAt);
}
