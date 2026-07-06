using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BMS.Application.Interfaces;

namespace BMS.API.Services;

public class CurrentUserService : ICurrentUserService
{
    public const string TenantIdClaimType = "tenant_id";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? UserId =>
        User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User?.FindFirstValue(JwtRegisteredClaimNames.Sub);

    public string? Role =>
        User?.FindFirstValue(ClaimTypes.Role);

    public int? TenantId
    {
        get
        {
            var raw = User?.FindFirstValue(TenantIdClaimType);
            return int.TryParse(raw, out var id) ? id : null;
        }
    }

    public bool IsViewer =>
        string.Equals(Role, "Viewer", StringComparison.OrdinalIgnoreCase);

    public bool IsAdminOrManager =>
        string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Role, "Manager", StringComparison.OrdinalIgnoreCase);
}
