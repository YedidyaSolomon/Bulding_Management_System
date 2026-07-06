using BMS.Application.Interfaces;
using BMS.Application.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BMS.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    public const string TenantIdClaimType = "tenant_id";

    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
    }

    public string GenerateToken(string userId, string email, string fullName, string role, int? tenantId = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   userId),
            new(ClaimTypes.NameIdentifier,     userId),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new(ClaimTypes.Name,               fullName),
            new(ClaimTypes.Role,               role)
        };

        if (tenantId.HasValue)
            claims.Add(new Claim(TenantIdClaimType, tenantId.Value.ToString()));

        var signingKey  = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             _settings.Issuer,
            audience:           _settings.Audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            GetExpiry(),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public DateTime GetExpiry() =>
        DateTime.UtcNow.AddMinutes(_settings.ExpiryInMinutes);
}
