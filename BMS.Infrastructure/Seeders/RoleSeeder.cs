using BMS.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BMS.Infrastructure.Seeders;

/// <summary>
/// Ensures the three application roles exist in the database.
/// This is idempotent — safe to call on every application startup.
/// Roles: Admin | Manager | Viewer
/// </summary>
public static class RoleSeeder
{
    private static readonly string[] Roles = { "Admin", "Manager", "Viewer" };

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger      = serviceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        foreach (var roleName in Roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (result.Succeeded)
                    logger.LogInformation("Role '{Role}' created.", roleName);
                else
                    logger.LogError("Failed to create role '{Role}': {Errors}",
                        roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
