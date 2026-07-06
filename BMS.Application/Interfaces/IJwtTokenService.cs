namespace BMS.Application.Interfaces;

public interface IJwtTokenService
{
    string   GenerateToken(string userId, string email, string fullName, string role, int? tenantId = null);
    DateTime GetExpiry();
}
