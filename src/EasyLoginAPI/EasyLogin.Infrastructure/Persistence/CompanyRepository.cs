using EasyLogin.Application.Companies.Dtos;
using EasyLogin.Application.Interfaces;
using EasyLogin.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyLogin.Infrastructure.Persistence;

public class CompanyRepository(AppDbContext db) : ICompanyRepository
{
    public async Task<IList<CompanyResponse>> GetAllAsync()
    {
        var companies = await db.Companies.OrderBy(c => c.Name).ToListAsync();
        return companies.Select(Map).ToList();
    }

    public async Task<CompanyResponse> GetByIdAsync(Guid id)
    {
        var company = await db.Companies.FindAsync(id)
            ?? throw new KeyNotFoundException($"Company {id} not found.");
        return Map(company);
    }

    public async Task<CompanyResponse> CreateAsync(string name)
    {
        var company = new Company { Name = name };
        db.Companies.Add(company);
        await db.SaveChangesAsync();
        return Map(company);
    }

    public async Task<CompanyResponse> UpdateAsync(Guid id, string name, bool isActive)
    {
        var company = await db.Companies.FindAsync(id)
            ?? throw new KeyNotFoundException($"Company {id} not found.");
        company.Name = name;
        company.IsActive = isActive;
        await db.SaveChangesAsync();
        return Map(company);
    }

    public async Task DeleteAsync(Guid id)
    {
        var company = await db.Companies.FindAsync(id)
            ?? throw new KeyNotFoundException($"Company {id} not found.");
        db.Companies.Remove(company);
        await db.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(Guid id)
        => await db.Companies.AnyAsync(c => c.Id == id);

    private static CompanyResponse Map(Company c)
        => new(c.Id, c.Name, c.IsActive, c.CreatedAt);
}
