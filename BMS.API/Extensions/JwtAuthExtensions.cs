using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace BMS.API.Extensions;

/// <summary>
/// Configures JWT Bearer authentication.
/// The signing key, issuer and audience are read from appsettings.json so
/// they can vary per environment without recompiling.
/// </summary>
public static class JwtAuthExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey   = jwtSettings["SecretKey"]
                          ?? throw new InvalidOperationException("JWT SecretKey is not configured.");

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey  = true,
                    ValidIssuer              = jwtSettings["Issuer"],
                    ValidAudience            = jwtSettings["Audience"],
                    IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ClockSkew                = TimeSpan.Zero   // no grace period on expiry
                };
            });

        return services;
    }
}
