using EasyLogin.Infrastructure.Identity;
using EasyLogin.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EasyLogin.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = sp.GetRequiredService<RoleManager<AppIdentityRole>>();
        var userManager = sp.GetRequiredService<UserManager<AppIdentityUser>>();
        var config = sp.GetRequiredService<IConfiguration>();
        var logger = sp.GetRequiredService<ILogger<AppDbContext>>();

        await SeedRolesAsync(roleManager, logger);
        await SeedAdminAsync(userManager, config, logger);
    }

    private static async Task SeedRolesAsync(RoleManager<AppIdentityRole> roleManager, ILogger logger)
    {
        string[] roles = ["SuperAdmin", "CompanyAdmin", "User"];
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

    private static async Task SeedAdminAsync(UserManager<AppIdentityUser> userManager, IConfiguration config, ILogger logger)
    {
        var adminEmail = config["ADMIN_EMAIL"];
        var adminPassword = config["ADMIN_PASSWORD"];

        if (string.IsNullOrWhiteSpace(adminEmail))
            throw new InvalidOperationException("ADMIN_EMAIL environment variable is required but not set.");
        if (string.IsNullOrWhiteSpace(adminPassword))
            throw new InvalidOperationException("ADMIN_PASSWORD environment variable is required but not set.");

        var existing = await userManager.FindByEmailAsync(adminEmail);
        if (existing is not null)
            return;

        var admin = new AppIdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "Admin",
            LastName = "User",
            EmailConfirmed = true,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = null
        };

        var result = await userManager.CreateAsync(admin, adminPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        await userManager.AddToRoleAsync(admin, "SuperAdmin");
        logger.LogInformation("Seeded admin user '{Email}'", adminEmail);
    }
}
