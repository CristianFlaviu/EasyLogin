using EasyLogin.Infrastructure.Identity;
using EasyLogin.Domain.Entities;
using EasyLogin.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EasyLogin.Infrastructure.Persistence;

public static class DataSeeder
{
    private const string AdminEmail = "admin@admin.com";
    private const string AdminPassword = "Today@123";

    private const string DefaultTenantName = "Default Tenant";
    private const string DefaultTenantUserEmail = "flaviu1@test.com";
    private const string DefaultTenantUserPassword = "Today@123";
    private const string DefaultTenantUserFirstName = "Flaviu";
    private const string DefaultTenantUserLastName = "Test";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = sp.GetRequiredService<RoleManager<AppIdentityRole>>();
        var userManager = sp.GetRequiredService<UserManager<AppIdentityUser>>();
        var logger = sp.GetRequiredService<ILogger<AppDbContext>>();

        await SeedRolesAsync(roleManager, logger);
        await SeedAdminAsync(userManager, logger);
        await SeedDefaultTenantAsync(db, userManager, logger);
    }

    private static async Task SeedRolesAsync(RoleManager<AppIdentityRole> roleManager, ILogger logger)
    {
        string[] roles = ["SuperAdmin", "TenantAdmin", "User"];
        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new AppIdentityRole
                {
                    Name = roleName,
                    IsSystemRole = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = null
                });

                if (!result.Succeeded)
                    throw new InvalidOperationException(
                        $"Failed to create role '{roleName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");

                logger.LogInformation("Seeded role '{Role}'", roleName);
            }
        }
    }

    private static async Task SeedAdminAsync(UserManager<AppIdentityUser> userManager, ILogger logger)
    {
        var existing = await userManager.FindByEmailAsync(AdminEmail);
        if (existing is not null)
            return;

        var admin = new AppIdentityUser
        {
            UserName = AdminEmail,
            Email = AdminEmail,
            FirstName = "Admin",
            LastName = "User",
            EmailConfirmed = true,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = null
        };

        var result = await userManager.CreateAsync(admin, AdminPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        await userManager.AddToRoleAsync(admin, "SuperAdmin");
        logger.LogInformation("Seeded admin user '{Email}'", AdminEmail);
    }

    private static async Task SeedDefaultTenantAsync(
        AppDbContext db,
        UserManager<AppIdentityUser> userManager,
        ILogger logger)
    {
        var tenant = await db.Tenants.SingleOrDefaultAsync(t => t.Name == DefaultTenantName);
        if (tenant is null)
        {
            tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = DefaultTenantName,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = null
            };

            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded default tenant '{Tenant}'", DefaultTenantName);
        }
        else if (!tenant.IsActive)
        {
            tenant.IsActive = true;
            tenant.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }

        var user = await userManager.FindByEmailAsync(DefaultTenantUserEmail);
        if (user is null)
        {
            user = new AppIdentityUser
            {
                UserName = DefaultTenantUserEmail,
                Email = DefaultTenantUserEmail,
                FirstName = DefaultTenantUserFirstName,
                LastName = DefaultTenantUserLastName,
                EmailConfirmed = true,
                Status = UserStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = null,
                TenantId = tenant.Id
            };

            var createResult = await userManager.CreateAsync(user, DefaultTenantUserPassword);
            if (!createResult.Succeeded)
                throw new InvalidOperationException(
                    $"Failed to create default tenant user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");

            logger.LogInformation("Seeded default tenant user '{Email}'", DefaultTenantUserEmail);
        }
        else if (user.TenantId != tenant.Id || user.Status != UserStatus.Active || !user.EmailConfirmed)
        {
            user.TenantId = tenant.Id;
            user.Status = UserStatus.Active;
            user.EmailConfirmed = true;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                throw new InvalidOperationException(
                    $"Failed to update default tenant user: {string.Join(", ", updateResult.Errors.Select(e => e.Description))}");
        }

        if (!await userManager.IsInRoleAsync(user, "TenantAdmin"))
        {
            var roleResult = await userManager.AddToRoleAsync(user, "TenantAdmin");
            if (!roleResult.Succeeded)
                throw new InvalidOperationException(
                    $"Failed to assign default tenant user role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
        }
    }
}
