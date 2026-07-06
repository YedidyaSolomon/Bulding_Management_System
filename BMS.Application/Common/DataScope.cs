using BMS.Application.Interfaces;

namespace BMS.Application.Common;

public static class DataScope
{
    public static void EnsureViewerTenantAccess(ICurrentUserService user, int resourceTenantId)
    {
        if (!user.IsViewer)
            return;

        if (!user.TenantId.HasValue || user.TenantId.Value != resourceTenantId)
            throw new KeyNotFoundException("Resource not found.");
    }

    public static void EnsureViewerUserAccess(ICurrentUserService user, string resourceUserId)
    {
        if (!user.IsViewer)
            return;

        if (string.IsNullOrEmpty(user.UserId) || user.UserId != resourceUserId)
            throw new KeyNotFoundException("Resource not found.");
    }

    public static string ResolveUserId(ICurrentUserService user, string requestedUserId)
    {
        if (user.IsViewer)
            return user.UserId ?? throw new UnauthorizedAccessException("User ID not found in token.");

        return requestedUserId;
    }
}
