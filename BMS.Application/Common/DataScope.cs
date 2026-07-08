using BMS.Application.Exceptions;
using BMS.Application.Interfaces;

namespace BMS.Application.Common;

public static class DataScope
{
    /// <summary>
    /// Guards a single-tenant resource.
    /// <para>
    /// <paramref name="ownedTenantIds"/> is the value returned by
    /// <see cref="ITenantOwnershipResolver.GetOwnedTenantIdsAsync"/>:
    /// <c>null</c> means the caller is Admin/Manager (bypass); a list means
    /// Viewer scope — only IDs in that list are accessible.
    /// </para>
    /// Throws <see cref="ForbiddenAccessException"/> (→ 403) when the
    /// resource's tenant is not in the viewer's ownership list.
    /// </summary>
    public static void EnsureViewerOwnedTenantAccess(List<int>? ownedTenantIds, int resourceTenantId)
    {
        if (ownedTenantIds is null)
            return; // Admin / Manager — bypass

        if (!ownedTenantIds.Contains(resourceTenantId))
            throw new ForbiddenAccessException();
    }

    /// <summary>
    /// Guards a user-scoped resource (e.g. Notifications).
    /// Viewer may only access resources whose <paramref name="resourceUserId"/>
    /// matches their own <see cref="ICurrentUserService.UserId"/>.
    /// </summary>
    public static void EnsureViewerUserAccess(ICurrentUserService user, string resourceUserId)
    {
        if (!user.IsViewer)
            return;

        if (string.IsNullOrEmpty(user.UserId) || user.UserId != resourceUserId)
            throw new KeyNotFoundException("Resource not found.");
    }

    /// <summary>
    /// For endpoints that accept a <c>userId</c> query parameter:
    /// Viewers are always redirected to their own ID; Admin/Manager pass through.
    /// </summary>
    public static string ResolveUserId(ICurrentUserService user, string requestedUserId)
    {
        if (user.IsViewer)
            return user.UserId ?? throw new UnauthorizedAccessException("User ID not found in token.");

        return requestedUserId;
    }
}
