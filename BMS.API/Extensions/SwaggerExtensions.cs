using Microsoft.OpenApi.Models;

namespace BMS.API.Extensions;

/// <summary>
/// Configures Swagger/OpenAPI with JWT Bearer support so developers can
/// authenticate directly in the Swagger UI without external tools.
/// </summary>
public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title       = "Building Management System API",
                Version     = "v1",
                Description = "Multi-Tenant Commercial Building Rental Management Platform"
            });

            // Add JWT security definition
            var securityScheme = new OpenApiSecurityScheme
            {
                Name         = "Authorization",
                Description  = "Enter: Bearer {your JWT token}",
                In           = ParameterLocation.Header,
                Type         = SecuritySchemeType.ApiKey,
                Scheme       = "Bearer",
                BearerFormat = "JWT"
            };

            c.AddSecurityDefinition("Bearer", securityScheme);

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id   = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}
