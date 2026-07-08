namespace BMS.Application.Interfaces;

public interface IJwtTokenService
{
    /// <summary>
    /// Generates a signed JWT containing sub/NameIdentifier, email, name, and role claims.
    /// No tenant ID is embedded — ownership is resolved at query time via ITenantOwnershipResolver.
    /// </summary>
    string   GenerateToken(string userId, string email, string fullName, string role);
    DateTime GetExpiry();
}
