using BMS.Application.Interfaces;
using BMS.Application.Interfaces.Repositories;
using BMS.Application.Settings;
using BMS.Infrastructure.Data;
using BMS.Infrastructure.Entities;
using BMS.Infrastructure.Repositories;
using BMS.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BMS.API.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly("BMS.Infrastructure")));

        // Identity
        services.AddIdentity<AppUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit           = true;
                options.Password.RequiredLength         = 8;
                options.Password.RequireUppercase       = true;
                options.Password.RequireLowercase       = true;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail         = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // JWT Settings + JwtTokenService (infrastructure concern)
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // ── Ownership resolver (scoped — one DB query per request, cached by DI) ──
        services.AddScoped<ITenantOwnershipResolver, TenantOwnershipResolver>();

        // ── Repository registrations ─────────────────────────────────────────
        services.AddScoped<IUserRepository,         UserRepository>();
        services.AddScoped<IUnitRepository,         UnitRepository>();
        services.AddScoped<ITenantRepository,       TenantRepository>();
        services.AddScoped<ILeaseRepository,        LeaseRepository>();
        services.AddScoped<IInvoiceRepository,      InvoiceRepository>();
        services.AddScoped<IPaymentRepository,      PaymentRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IDocumentRepository,     DocumentRepository>();

        return services;
    }
}
