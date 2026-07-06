namespace BMS.Application.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? Role { get; }
    int? TenantId { get; }
    bool IsViewer { get; }
    bool IsAdminOrManager { get; }
}
