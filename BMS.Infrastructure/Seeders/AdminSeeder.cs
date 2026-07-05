using BMS.Infrastructure.Data;
using BMS.Infrastructure.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BMS.Infrastructure.Seeders;

/// <summary>
/// Creates the default Administrator account if it does not already exist.
/// Credentials are read from appsettings.json → AdminSeed section so they
/// can be changed per environment without modifying code.
///
/// Default credentials (development):
///   Email:    admin@bms.com
///   Password: Admin@12345
/// </summary>2
public static class AdminSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
        var logger      = serviceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        var adminSection = configuration.GetSection("AdminSeed");
        var email        = adminSection["Email"]    ?? "admin@bms.com";
        var password     = adminSection["Password"] ?? "Admin@12345";
        var fullName     = adminSection["FullName"] ?? "System Administrator";
        var role         = adminSection["Role"]     ?? "Admin";

        var existingAdmin = await userManager.FindByEmailAsync(email);
        if (existingAdmin is not null)
            return; // Already seeded — nothing to do

        var admin = new AppUser
        {
            FullName = fullName,
            Email    = email,
            UserName = email,
            IsActive = true
        };

        var createResult = await userManager.CreateAsync(admin, password);
        if (!createResult.Succeeded)
        {
            logger.LogError("Admin seed failed: {Errors}",
                string.Join(", ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        var roleResult = await userManager.AddToRoleAsync(admin, role);
        if (roleResult.Succeeded)
            logger.LogInformation("Default admin account seeded: {Email}", email);
        else
            logger.LogError("Admin role assignment failed: {Errors}",
                string.Join(", ", roleResult.Errors.Select(e => e.Description)));
    }
}
