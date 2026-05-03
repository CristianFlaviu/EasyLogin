using EasyLogin.Application.Companies.Dtos;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyLogin.Infrastructure.Persistence;

public class CompanyRoleRepository(AppDbContext db) : ICompanyRoleRepository
{
    public async Task<IList<CompanyRoleResponse>> GetByCompanyIdAsync(Guid companyId)
    {
        var roles = await db.CompanyRoles
            .Where(cr => cr.CompanyId == companyId)
            .OrderBy(cr => cr.Name)
            .ToListAsync();
        return roles.Select(Map).ToList();
    }

    public async Task<CompanyRoleResponse> CreateAsync(string name, string? description, Guid companyId)
    {
        var exists = await db.CompanyRoles.AnyAsync(cr => cr.Name == name && cr.CompanyId == companyId);
        if (exists)
            throw new InvalidOperationException($"Role '{name}' already exists in this company.");

        var role = new CompanyRole { Name = name, Description = description, CompanyId = companyId };
        db.CompanyRoles.Add(role);
        await db.SaveChangesAsync();
        return Map(role);
    }

    public async Task DeleteAsync(Guid id, Guid requiredCompanyId)
    {
        var role = await db.CompanyRoles.FindAsync(id)
            ?? throw new KeyNotFoundException($"Role {id} not found.");

        if (role.CompanyId != requiredCompanyId)
            throw new UnauthorizedAccessException();

        var inUse = await db.UserCompanyRoles.AnyAsync(ucr => ucr.CompanyRoleId == id);
        if (inUse)
            throw new InvalidOperationException($"Role '{role.Name}' is assigned to one or more users and cannot be deleted.");

        db.CompanyRoles.Remove(role);
        await db.SaveChangesAsync();
    }

    public async Task<IList<string>> GetUserRoleNamesAsync(string userId)
        => await db.UserCompanyRoles
            .Where(ucr => ucr.UserId == userId)
            .Join(db.CompanyRoles, ucr => ucr.CompanyRoleId, cr => cr.Id, (_, cr) => cr.Name)
            .ToListAsync();

    public async Task UpdateUserRolesAsync(string userId, IList<Guid> companyRoleIds, Guid callerCompanyId)
    {
        if (companyRoleIds.Count > 0)
        {
            var validIds = await db.CompanyRoles
                .Where(cr => companyRoleIds.Contains(cr.Id) && cr.CompanyId == callerCompanyId)
                .Select(cr => cr.Id)
                .ToListAsync();

            var invalid = companyRoleIds.Except(validIds).ToList();
            if (invalid.Count > 0)
                throw new InvalidOperationException("One or more roles do not belong to this company.");
        }

        var existing = await db.UserCompanyRoles
            .Where(ucr => ucr.UserId == userId)
            .Join(db.CompanyRoles, ucr => ucr.CompanyRoleId, cr => cr.Id,
                (ucr, cr) => new { ucr, cr.CompanyId })
            .Where(x => x.CompanyId == callerCompanyId)
            .Select(x => x.ucr)
            .ToListAsync();

        db.UserCompanyRoles.RemoveRange(existing);

        foreach (var roleId in companyRoleIds)
            db.UserCompanyRoles.Add(new UserCompanyRole { UserId = userId, CompanyRoleId = roleId });

        await db.SaveChangesAsync();
    }

    private static CompanyRoleResponse Map(CompanyRole cr)
        => new(cr.Id, cr.Name, cr.Description, cr.CompanyId, cr.CreatedAt);
}
